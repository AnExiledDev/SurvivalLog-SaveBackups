using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ExiledToolkit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using SaveBackups.Core;
using UnityEngine;

namespace SaveBackups.Adapters;

/// <summary>
/// The mod's game side. Hooks and the page only ask for things; the game is read and touched here on the main thread,
/// and the copying runs on the disk worker. A step on a run (restore, delete, bring back) always makes a safety backup
/// first and does nothing more if that fails.
/// </summary>
internal sealed class Session
{
    /// <summary>The morning save is written a frame after the day changes; the backup waits this long for it.</summary>
    private const double MorningDelaySeconds = 1.5;

    private static readonly TimeSpan QuitWait = TimeSpan.FromSeconds(8);
    private static readonly Func<KeyCode, bool> IsKeyDown = Input.GetKeyDown;

    /// <summary>How often play time is counted, and how often what was counted is written to the run's note.</summary>
    private const double PlayCheckSeconds = 1;
    private const double PlayWriteSeconds = 60;

    /// <summary>A gap longer than this between checks (a hitch, the game in the background) is not counted as play.</summary>
    private const double MostPlayStep = 5;

    /// <summary>A timer backup that finds the game unable to save tries again this much later.</summary>
    private const double TimerRetrySeconds = 30;

    /// <summary>Saves read for their stats each time a detail view opens; the rest are read the next time.</summary>
    private const int StatsReadsPerDetail = 12;

    private readonly GameSaves _game;
    private readonly WebPage _page;
    private readonly DiskWorker _worker;
    private readonly Action<string> _log;
    private readonly Func<string, string, string?> _setSetting;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingValues;
    private readonly HashSet<string> _warnedBehind = new(StringComparer.OrdinalIgnoreCase);
    private readonly WebKeyboard _keyboard = new();

    private Settings _settings;
    private BackupStore _store;
    private KeyCode _backupKey = KeyCode.None;
    private State_Web_MainUI? _mainState;
    private bool _isOpen;
    private bool _isGameListNext;
    private double _morningDueAt;
    private RunFacts? _leaving;
    private bool _isTitleCheckDue = true;
    private string _inRunStem = "";
    private Dictionary<int, Survivor>? _survivors;
    private string? _playStem;
    private double _playCheckAt;
    private double _lastPlayCheck;
    private double _playedUnwritten;
    private double _playWriteAt;
    private double _timerPlayed;
    private double _timerRetryAt;

    public Session(GameSaves game, WebPage page, DiskWorker worker, Settings settings, Action<string> log,
        Func<string, string, string?> setSetting, Func<IReadOnlyDictionary<string, string>> settingValues)
    {
        _game = game;
        _page = page;
        _worker = worker;
        _settings = settings;
        _log = log;
        _setSetting = setSetting;
        _settingValues = settingValues;
        _store = new BackupStore(settings.Root, () => DateTime.UtcNow);
    }

    public void UseSettings(Settings settings)
    {
        if (!string.Equals(settings.Root, _settings.Root, StringComparison.Ordinal)) _store = new BackupStore(settings.Root, () => DateTime.UtcNow);

        _settings = settings;
        _log($"backups in {settings.Root}; morning {settings.OnMorning}, quit {settings.OnQuit}, rebirth {settings.BeforeRebirth}, expedition {settings.BeforeExpedition}, wave {settings.BeforeWave}, ending {settings.AtEnding}, timer {(settings.OnTimer ? settings.TimerMinutes + " min" : "off")}, pictures {settings.TakePictures}, verify {settings.Verify}, second folder {(settings.SecondFolder.Length > 0 ? settings.SecondFolder : "none")}, bin {settings.BinDays} days, load screen {settings.ReplaceLoadScreen}");
        Refresh();
    }

    public void UseKey(KeyCode key) => _backupKey = key;

    /// <summary>Clears what a crash left half-written, before any backup is made.</summary>
    public void Start()
    {
        var store = _store;
        var binDays = _settings.BinDays;

        _worker.Run(() =>
        {
            Directory.CreateDirectory(store.ImportPath);

            return (Swept: store.SweepPartials(), Expired: store.ExpireBin(binDays));
        }, done =>
        {
            if (done.Swept > 0) _log($"removed {done.Swept} unfinished backup(s) left by a crash");
            if (done.Expired > 0) _log($"{done.Expired} backup(s) were in the bin longer than {binDays} days and are deleted for good");
        }, ex => _log("could not clear unfinished backups: " + ex.Message));
    }

    public void Update()
    {
        var now = Clock.Now;

        _page.KeepInstalled(now);
        _worker.Drain();
        TakePageCommands();

        if (_morningDueAt > 0 && now >= _morningDueAt)
        {
            _morningDueAt = 0;
            BackUpRunning(BackupKind.Morning, isAsked: false);
        }

        if (now >= _playCheckAt) CountPlay(now);

        if (_isTitleCheckDue && _page.IsReady) CheckTitle();
        if (!Input.anyKeyDown) return;
        if (_isOpen && !_keyboard.IsClaimed && Input.GetKeyDown(KeyCode.Escape)) _page.Escape();
        // A key typed into a text box, here or in another mod's window, is text, not a backup.
        if (KeyGate.Fires(_backupKey, KeyCode.None, IsKeyDown, OptionalLive.IsTyping) && _game.CurrentRun() != null) BackUpRunning(BackupKind.Manual, isAsked: true);
    }

    /// <summary>
    /// Once a second: adds the time since the last check to the run being played, writes it to the run's note about
    /// once a minute, and makes the timer backup when its minutes of play are up.
    /// </summary>
    private void CountPlay(double now)
    {
        _playCheckAt = now + PlayCheckSeconds;

        var step = Math.Min(now - _lastPlayCheck, MostPlayStep);
        var stem = GameSaves.CurrentFile() is { } file ? RunFiles.Stem(file) : null;

        _lastPlayCheck = now;

        if (!string.Equals(stem, _playStem, StringComparison.OrdinalIgnoreCase))
        {
            WritePlayed();
            _playStem = stem;
            _timerPlayed = 0;
            _timerRetryAt = 0;
            _playWriteAt = now + PlayWriteSeconds;

            return;
        }

        if (stem == null) return;

        _playedUnwritten += step;
        _timerPlayed += step;

        if (now >= _playWriteAt)
        {
            _playWriteAt = now + PlayWriteSeconds;
            WritePlayed();
        }

        if (!_settings.OnTimer || _timerPlayed < _settings.TimerMinutes * 60.0 || now < _timerRetryAt) return;

        if (!_game.IsSafeToSave())
        {
            _timerRetryAt = now + TimerRetrySeconds;

            return;
        }

        _timerPlayed = 0;
        _game.SaveNow();

        if (_game.CurrentRun() is { } facts) BackUp(facts, BackupKind.Timer, isAsked: false);
    }

    /// <summary>Adds the play time counted since the last write to the run's note, on the worker.</summary>
    private void WritePlayed()
    {
        var stem = _playStem;
        var seconds = (long)_playedUnwritten;

        if (stem == null || seconds <= 0) return;

        _playedUnwritten -= seconds;

        var store = _store;

        _worker.Run(
            () => store.UpdateNote(stem, note => note with { PlayedSeconds = note.PlayedSeconds + seconds }),
            done =>
            {
                if (done is Result<Unit>.Failed failed) _log($"could not keep the play time of {stem}: {failed.Why}");
            },
            ex => _log($"could not keep the play time of {stem}: {ex.Message}"));
    }

    // ===== Game moments =====

    public void MorningSaved()
    {
        if (_settings.OnMorning) _morningDueAt = Clock.Now + MorningDelaySeconds;
    }

    /// <summary>Before the game saves on the way out of a run: the run's facts, while it is still loaded.</summary>
    public void LeavingRun()
    {
        _morningDueAt = 0;
        _leaving = _settings.OnQuit ? _game.CurrentRun() : null;
        WritePlayed();
    }

    /// <summary>After the game saved on the way out. At game quit the backup is finished before the game closes.</summary>
    public void LeftRun(bool wait)
    {
        var facts = _leaving;

        _leaving = null;
        _isTitleCheckDue = true;

        if (facts == null) return;

        GameSaves.Flush();
        BackUp(facts, BackupKind.Quit, isAsked: false);

        if (wait && !_worker.WaitIdle(QuitWait)) _log("the backup at quit did not finish in time");
    }

    public void RebirthStarting()
    {
        if (!_settings.BeforeRebirth) return;

        var facts = _game.CurrentRun();

        if (facts == null) return;

        GameSaves.Flush();
        BackUp(facts, BackupKind.Rebirth, isAsked: false, settleAfter: true);

        // The rebirth writes the run's files next, so the copy is finished first.
        if (!_worker.WaitIdle(QuitWait)) _log("the backup before the rebirth did not finish in time");
    }

    /// <summary>Before the survivor sets out: the run is saved (when the game allows it) and copied before the map loads.</summary>
    public void ExpeditionStarting()
    {
        if (!_settings.BeforeExpedition) return;

        if (SaveForBackup() is not { } facts) return;

        BackUp(facts, BackupKind.Expedition, isAsked: false);

        // Leaving for the map can write the run's files, so the copy is finished first.
        if (!_worker.WaitIdle(QuitWait)) _log("the backup before the expedition did not finish in time");
    }

    public void WaveStarting()
    {
        if (!_settings.BeforeWave) return;

        if (SaveForBackup() is { } facts) BackUp(facts, BackupKind.Wave, isAsked: false);
    }

    /// <summary>
    /// After an ending line's confirm step. Only a confirm that marked a new ending choice is the ending: the game
    /// saved the run there, so a backup now can be restored to try another ending.
    /// </summary>
    public void EndingConfirmed(int pendingBefore)
    {
        if (!_settings.AtEnding) return;

        var pending = GameSaves.PendingEndingId();

        if (pending == 0 || pending == pendingBefore) return;

        GameSaves.Flush();

        var facts = _game.CurrentRun();

        if (facts == null) return;

        _log($"ending choice {pending} saved");
        BackUp(facts, BackupKind.Ending, isAsked: false);
    }

    /// <summary>Saves the run when the game would itself, else writes out what it queued; the run's facts after.</summary>
    private RunFacts? SaveForBackup()
    {
        if (_game.CurrentRun() == null) return null;

        if (_game.IsSafeToSave()) _game.SaveNow();
        else GameSaves.Flush();

        return _game.CurrentRun();
    }

    // ===== The manager =====

    /// <summary>Called from the Load button's hook: true when the mod's manager opened and the game's list must not.</summary>
    public bool OpenInsteadOfGameList(State_Web_MainUI state)
    {
        if (_isGameListNext)
        {
            _isGameListNext = false;

            return false;
        }

        if (!_settings.ReplaceLoadScreen || !_page.IsReady) return false;
        if (!_game.HasSaveList() && !HasAnyBackups()) return false;

        _mainState = state;
        _isOpen = true;
        HotkeyGate.MenuOpened();
        Refresh(isOpening: true);

        return true;
    }

    private bool HasAnyBackups()
    {
        try
        {
            return _store.HasRuns();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>From the pause menu's button: the manager over the run, with the run being played marked and kept safe.</summary>
    private void OpenInRun()
    {
        var file = GameSaves.CurrentFile();

        if (_isOpen || file == null || !_page.IsReady) return;

        _inRunStem = RunFiles.Stem(file);
        _mainState = null;
        _isOpen = true;
        HotkeyGate.MenuOpened();
        Refresh(isOpening: true);
    }

    private void Close()
    {
        _keyboard.Release();
        _isOpen = false;
        _inRunStem = "";
        HotkeyGate.MenuClosed();
        _page.Close();
    }

    private bool IsInRun => _inRunStem.Length > 0;

    /// <summary>True, with a toast, when the step would change the run being played under the game.</summary>
    private bool IsPlaying(string stem)
    {
        if (!string.Equals(stem, _inRunStem, StringComparison.OrdinalIgnoreCase)) return false;

        _page.Toast(Texts.InRunBlocked.Pick(GameLanguage.IsChinese()), ToastKind.Warning);
        _page.Done();

        return true;
    }

    /// <summary>Reads the game's list here, then the backups on the worker, and sends the page the whole view.</summary>
    private void Refresh(bool isOpening = false)
    {
        if (!_isOpen) return;

        var state = _mainState;
        var runs = IsInRun ? _game.HistoryRuns() : state == null ? new List<RunView>() : _game.LoadScreenRuns(state);
        var store = _store;
        var key = _backupKey == KeyCode.None ? "" : _backupKey.ToString();
        var isChinese = GameLanguage.IsChinese();
        var version = _game.Version();
        var limit = _settings.Limits.MaxTotalBytes;
        var binDays = _settings.BinDays;
        var saveFolder = _game.SaveFolder();
        var inRun = _inRunStem;
        var values = _settingValues();
        var isListKnown = _game.HasHistory();

        // The game's table is read once it has loaded; an empty answer is asked again next time.
        if (_survivors == null || _survivors.Count == 0) _survivors = SaveReader.Survivors();

        var survivors = _survivors;

        _worker.Run(
            () =>
            {
                var backups = store.All();
                var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var disk = new Dictionary<string, SaveDisk>(StringComparer.OrdinalIgnoreCase);

                foreach (var run in runs)
                {
                    var stem = RunFiles.Stem(run.FileName);

                    listed.Add(stem);
                    if (saveFolder != null) disk[stem] = Disk(saveFolder, stem);
                }

                // A run that left the game's list starts its days in the bin now; one that came back stops them.
                if (isListKnown)
                {
                    var notes = store.AllNotes();

                    foreach (var stem in backups.Select(meta => meta.Stem).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        var isMarked = notes.TryGetValue(stem, out var note) && note.DeletedAt != null;

                        if (!listed.Contains(stem) && !isMarked) store.MarkDeleted(stem, DateTime.UtcNow);
                        else if (listed.Contains(stem) && isMarked) store.MarkListed(stem);
                    }
                }

                return PageModel.Json(new Core.Screen(runs, backups, store.AllSettled(), store.Root, key, isChinese, version, limit)
                {
                    Notes = store.AllNotes(),
                    Bin = store.Bin(),
                    BinDays = binDays,
                    ShotsRoot = FolderUrl(store.Root),
                    Survivors = survivors,
                    Settings = values,
                    InRunStem = inRun,
                    Disk = disk,
                    NowUtc = DateTime.UtcNow,
                });
            },
            json =>
            {
                if (!_isOpen) return;
                if (isOpening) _page.Open(json);
                else _page.Show(json);
            },
            ex =>
            {
                _log("could not read the backups: " + ex);
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);

                if (isOpening) Close();
                else _page.Done();
            });
    }

    /// <summary>A run's save files together, and when its main file was last written.</summary>
    private static SaveDisk Disk(string saveFolder, string stem)
    {
        long bytes = 0;
        var written = DateTime.MinValue;

        foreach (var name in RunFiles.In(saveFolder, stem))
        {
            var info = new FileInfo(Path.Combine(saveFolder, name));

            bytes += info.Length;
            if (string.Equals(name, RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)) written = info.LastWriteTimeUtc;
        }

        return new SaveDisk(bytes, written);
    }

    /// <summary>A folder as a file:// URL ending in "/", for the page to show the pictures in it.</summary>
    private static string FolderUrl(string folder)
    {
        try
        {
            var url = new Uri(Path.GetFullPath(folder)).AbsoluteUri;

            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }
        catch (UriFormatException)
        {
            return "";
        }
    }

    // ===== Backups =====

    private void BackUpRunning(BackupKind kind, bool isAsked)
    {
        var isChinese = GameLanguage.IsChinese();

        if (isAsked)
        {
            if (!_game.IsSafeToSave())
            {
                _page.Toast(Texts.NotNow.Pick(isChinese), ToastKind.Warning);

                return;
            }

            _game.SaveNow();
        }
        else
        {
            GameSaves.Flush();
        }

        var facts = _game.CurrentRun();

        if (facts == null)
        {
            if (isAsked) _page.Toast(Texts.NoRun.Pick(isChinese), ToastKind.Warning);

            return;
        }

        BackUp(facts, kind, isAsked, dayText: kind == BackupKind.Morning ? _game.DayText() : "");
    }

    /// <summary>
    /// Copies the run's files on the worker, then trims the backups to the limits and fills the second folder. The
    /// picture and the stats are read here first, on the main thread: the picture only of the run on screen.
    /// </summary>
    private void BackUp(RunFacts facts, BackupKind kind, bool isAsked, bool settleAfter = false, string dayText = "")
    {
        var saveFolder = _game.SaveFolder();

        if (saveFolder == null)
        {
            _log($"{BackupKinds.Id(kind)} backup of {facts.FileName} skipped: the save folder is not known yet");

            return;
        }

        var store = _store;
        var limits = _settings.Limits;
        var version = _game.Version();
        var stem = RunFiles.Stem(facts.FileName);
        var verify = _settings.Verify;
        var second = _settings.SecondFolder;
        var isCurrent = string.Equals(GameSaves.CurrentFile(), facts.FileName, StringComparison.OrdinalIgnoreCase);
        var noShot = "";
        byte[]? shot = null;

        if (_settings.TakePictures)
        {
            if (isCurrent) shot = ShotTaker.Take(out noShot);
            else noShot = "the run is not on screen";
        }

        var stats = isCurrent ? SaveReader.Current() : null;

        stats ??= SaveReader.OfFile(Path.Combine(saveFolder, facts.FileName));

        _worker.Run(
            () =>
            {
                if (Repeats.IsRepeat(kind, facts.Progress, store.Of(stem), DateTime.UtcNow)) return (Made: (Result<BackupMeta>?)null, Mirror: (Result<int>?)null);

                var made = store.Create(saveFolder, facts, kind, version, dayText, shot, verify, stats);
                Result<int>? mirror = null;

                if (made is Result<BackupMeta>.Done done)
                {
                    if (settleAfter) store.Settle(stem, done.Value.CreatedUtc);

                    store.Prune(limits);

                    if (second.Length > 0) mirror = store.Mirror(second);
                }

                return (Made: made, Mirror: mirror);
            },
            answer =>
            {
                if (answer.Mirror is Result<int>.Failed failed)
                {
                    _log($"second copy in {second} failed: {failed.Why}");
                    _page.Toast(Texts.Format(Texts.MirrorFailed, GameLanguage.IsChinese(), failed.Why), ToastKind.Warning, 10);
                }

                if (isAsked && answer.Made is Result<BackupMeta>.Done && _settings.TakePictures && shot == null)
                {
                    _log("no picture with the backup: " + noShot);
                    _page.Toast(Texts.Format(Texts.NoPicture, GameLanguage.IsChinese(), noShot), ToastKind.Info);
                }

                Backed(facts, kind, isAsked, answer.Made);
            },
            ex => Backed(facts, kind, isAsked, Result.Failed<BackupMeta>(ex.ToString())));
    }

    private void Backed(RunFacts facts, BackupKind kind, bool isAsked, Result<BackupMeta>? made)
    {
        var isChinese = GameLanguage.IsChinese();

        switch (made)
        {
            case null:
                _log($"{BackupKinds.Id(kind)} backup of {facts.FileName} skipped: loop {facts.Progress.Turn} day {facts.Progress.Day + 1} already has one");
                break;
            case Result<BackupMeta>.Done done:
                _log($"{BackupKinds.Id(kind)} backup {done.Value.Stem}/{done.Value.Id}, day {facts.Progress.Day + 1}, {done.Value.Bytes / 1024} KB{(done.Value.HasShot ? ", with a picture" : "")}");

                if (isAsked || _settings.AutoBackupToasts) _page.Toast(Texts.Format(Texts.BackedUp, isChinese, facts.Name, facts.Progress.Day + 1), ToastKind.Success);

                break;
            case Result<BackupMeta>.Failed failed:
                _log($"{BackupKinds.Id(kind)} backup of {facts.FileName} failed: {failed.Why}");
                _page.Toast((failed.Why.Contains("does not match", StringComparison.Ordinal) ? Texts.VerifyFailed : Texts.Failed).Pick(isChinese), ToastKind.Error);
                break;
        }

        Refresh();
    }

    /// <summary>Once per run a session, a toast when its save is behind its last backup, so the player knows before opening Load.</summary>
    private void CheckTitle()
    {
        var store = _store;

        if (_game.CurrentRun() != null || _game.SaveFolder() == null || !_game.HasHistory()) return;

        _isTitleCheckDue = false;

        if (!_settings.WarnWhenBehind) return;

        var runs = _game.History();

        _worker.Run(
            () =>
            {
                var behind = new List<(RunFacts Run, BackupMeta Backup)>();

                foreach (var run in runs)
                {
                    var stem = RunFiles.Stem(run.FileName);

                    if (CloudCheck.Behind(run.Progress, store.Of(stem), store.SettledAt(stem)) is { } ahead) behind.Add((run, ahead));
                }

                return behind;
            },
            behind =>
            {
                foreach (var (run, backup) in behind)
                {
                    if (!_warnedBehind.Add(run.FileName)) continue;

                    _log($"{run.FileName} is at loop {run.Progress.Turn} day {run.Progress.Day + 1}, behind backup {backup.Id} (loop {backup.Run.Progress.Turn} day {backup.Run.Progress.Day + 1})");
                    _page.Toast(Texts.Format(Texts.Behind, GameLanguage.IsChinese(), run.Name, backup.Run.Progress.Day + 1), ToastKind.Warning, 12);
                }
            },
            ex => _log("could not compare saves with their backups: " + ex.Message));
    }

    // ===== Page commands =====

    private void TakePageCommands()
    {
        while (PageMessages.TryTake(out var command))
        {
            try
            {
                Handle(command);
            }
            catch (Exception ex)
            {
                _log($"page command {command} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                _page.Done();
            }
        }
    }

    private void Handle(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.Ready:
                _log("page script ready");
                _page.IsReady = true;
                _keyboard.Release();

                // A reloaded page has no manager on screen, and one left marked open would hold the game's hotkeys.
                if (_isOpen)
                {
                    _isOpen = false;
                    HotkeyGate.MenuClosed();
                }

                break;
            case PageCommand.NoScript:
                _page.IsReady = false;
                _page.Install();
                break;
            case PageCommand.Close:
                Close();
                break;
            case PageCommand.Load or PageCommand.GameList or PageCommand.NewGame when IsInRun:
                IsPlaying(_inRunStem);
                break;
            case PageCommand.Load load:
                Close();
                _log("loading " + load.FileName);
                GameSaves.Enter(load.FileName);
                break;
            case PageCommand.GameList:
                Close();
                _isGameListNext = true;
                Ac_MainUI_LoadGame.SendAction();
                break;
            case PageCommand.OpenFolder folder:
                OpenFolder(folder.Stem);
                break;
            case PageCommand.NewGame:
                Close();
                _log("new game");
                Ac_MainUI_NewGame.SendAction();
                break;
            case PageCommand.OpenInRun:
                OpenInRun();
                break;
            case PageCommand.BackUpNow:
                if (_game.CurrentRun() != null) BackUpRunning(BackupKind.Manual, isAsked: true);
                else _page.Done();
                break;
            case PageCommand.BackUp backUp:
                BackUpListed(backUp.Stem);
                break;
            case PageCommand.Restore restore when IsPlaying(restore.Stem):
            case PageCommand.DeleteSave delete when IsPlaying(delete.Stem):
            case PageCommand.Rename rename when IsPlaying(rename.Stem):
            case PageCommand.BringBack bringBack when IsPlaying(bringBack.Stem):
                break;
            case PageCommand.Restore restore:
                Restore(restore.Stem, restore.Id, isDeleted: false);
                break;
            case PageCommand.BringBack bringBack:
                Restore(bringBack.Stem, bringBack.Id, isDeleted: !_game.IsListed(RunFiles.MainFile(bringBack.Stem)));
                break;
            case PageCommand.DeleteSave delete:
                DeleteSave(delete.Stem);
                break;
            case PageCommand.Pin pin:
                OnDisk(() => _store.SetPinned(pin.Stem, pin.Id, pin.IsPinned), "pin " + pin.Id);
                break;
            case PageCommand.Remove remove:
                OnDisk(() => _store.Delete(remove.Stem, remove.Id), "remove " + remove.Id);
                break;
            case PageCommand.Dismiss dismiss:
                OnDisk(() => _store.Settle(dismiss.Stem, DateTime.UtcNow), "dismiss " + dismiss.Stem);
                break;
            case PageCommand.Note note:
                OnDisk(() => _store.SetNote(note.Stem, note.Id, note.Text), "note on " + note.Id);
                break;
            case PageCommand.CopyAsNew copy:
                CopyAsNew(copy.Stem, copy.Id);
                break;
            case PageCommand.Export export:
                Export(export.Stem, export.Id);
                break;
            case PageCommand.Import:
                Import();
                break;
            case PageCommand.ExportRun exportRun:
                ExportRun(exportRun.Stem, exportRun.IsAll, exportRun.WithExtras);
                break;
            case PageCommand.ImportList:
                ImportList();
                break;
            case PageCommand.ImportFile importFile:
                ImportFile(importFile.File);
                break;
            case PageCommand.Rename rename:
                Rename(rename.Stem, rename.Name);
                break;
            case PageCommand.RunPin runPin:
                OnDisk(() => _store.UpdateNote(runPin.Stem, note => note with { IsPinned = runPin.IsPinned }), "star " + runPin.Stem);
                break;
            case PageCommand.SaveNote saveNote:
                OnDisk(() => _store.UpdateNote(saveNote.Stem, note => note with { Note = Notes.Clean(saveNote.Text) }), "note on " + saveNote.Stem);
                break;
            case PageCommand.Duplicate duplicate:
                Duplicate(duplicate.Stem);
                break;
            case PageCommand.Detail detail:
                Detail(detail.Stem);
                break;
            case PageCommand.SnapshotAsNew snap:
                SnapshotAsNew(snap.Stem, snap.File);
                break;
            case PageCommand.BinPutBack putBack:
                OnDisk(() => _store.PutBack(putBack.Stem, putBack.Id), "put back " + putBack.Id, Texts.PutBack, putBack.Stem);
                break;
            case PageCommand.BinDelete binDelete:
                OnDisk(() => _store.DeleteFromBin(binDelete.Stem, binDelete.Id), "delete " + binDelete.Id + " from the bin", Texts.BinDeleted);
                break;
            case PageCommand.BinDeleteRun binRun:
                OnDisk(() =>
                {
                    _store.DeleteRunForever(binRun.Stem, isPinnedToo: true);

                    return Result.Done(new Unit());
                }, "delete the backups of " + binRun.Stem, Texts.BinDeleted);
                break;
            case PageCommand.BinEmpty:
                EmptyBin();
                break;
            case PageCommand.Set set:
                SetSetting(set.Id, set.Value);
                break;
            case PageCommand.Typing typing:
                if (typing.IsTyping && _isOpen) _keyboard.Claim();
                else _keyboard.Release();
                break;
            case PageCommand.Unknown unknown:
                _log("page said: " + unknown.Text);
                break;
            default:
                throw new InvalidOperationException("unhandled page command " + command);
        }
    }

    private RunFacts? Listed(string stem)
    {
        var fileName = RunFiles.MainFile(stem);

        foreach (var run in _game.History())
            if (string.Equals(run.FileName, fileName, StringComparison.OrdinalIgnoreCase)) return run;

        return null;
    }

    private void BackUpListed(string stem)
    {
        var run = Listed(stem);

        if (run == null)
        {
            _page.Done();

            return;
        }

        BackUp(run, BackupKind.Manual, isAsked: true);
    }

    /// <summary>
    /// A backup's files in place of the run's. A listed run gets a safety backup first; a deleted one has no files to
    /// keep, and goes back into the game's list once its files are in place.
    /// </summary>
    private void Restore(string stem, string id, bool isDeleted)
    {
        var saveFolder = _game.SaveFolder();
        var listed = isDeleted ? null : Listed(stem);

        if (saveFolder == null || (!isDeleted && listed == null))
        {
            _page.Done();

            return;
        }

        var store = _store;
        var version = _game.Version();

        _worker.Run(
            () =>
            {
                var backup = store.Find(stem, id);

                if (backup == null) return (Backup: (BackupMeta?)null, Result: Result.Failed<Unit>("no backup " + id));

                if (listed != null && store.Create(saveFolder, listed, BackupKind.Safety, version) is Result<BackupMeta>.Failed safety)
                    return (Backup: backup, Result: Result.Failed<Unit>("the safety backup failed, nothing was changed: " + safety.Why));

                var restored = store.Restore(stem, id, saveFolder);

                if (restored is Result<Unit>.Done) store.Settle(stem, DateTime.UtcNow);

                return (Backup: backup, Result: restored);
            },
            answer =>
            {
                var isChinese = GameLanguage.IsChinese();

                if (answer.Result is Result<Unit>.Failed failed || answer.Backup == null)
                {
                    _log($"restore of {stem}/{id} failed: {(answer.Result as Result<Unit>.Failed)?.Why}");
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                    Refresh();

                    return;
                }

                var backup = answer.Backup;
                var fileName = RunFiles.MainFile(stem);

                if (isDeleted)
                {
                    if (!_game.IsListed(fileName)) _game.AddEntry(fileName, backup.Run.Name);

                    _page.Toast(Texts.Format(Texts.BroughtBack, isChinese, backup.Run.Name), ToastKind.Success);
                }
                else
                {
                    _page.Toast(Texts.Format(Texts.Restored, isChinese, backup.Run.Name, backup.Run.Progress.Day + 1), ToastKind.Success);
                }

                _game.RefreshEntry(fileName);
                _log($"restored {stem}/{id} (day {backup.Run.Progress.Day + 1}){(isDeleted ? " as a deleted save" : "")}");
                Refresh();
            },
            ex =>
            {
                _log($"restore of {stem}/{id} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    /// <summary>A safety backup first, then the game's own delete; the backups stay and show under Deleted saves.</summary>
    private void DeleteSave(string stem)
    {
        var saveFolder = _game.SaveFolder();
        var listed = Listed(stem);

        if (saveFolder == null || listed == null)
        {
            _page.Done();

            return;
        }

        var store = _store;
        var version = _game.Version();

        _worker.Run(
            () => store.Create(saveFolder, listed, BackupKind.Safety, version),
            made =>
            {
                var isChinese = GameLanguage.IsChinese();

                if (made is Result<BackupMeta>.Failed failed)
                {
                    _log($"delete of {listed.FileName} stopped, the safety backup failed: {failed.Why}");
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                    Refresh();

                    return;
                }

                var (isDone, why) = _game.Delete(listed.FileName);

                _log(isDone ? "deleted " + listed.FileName : $"the game would not delete {listed.FileName}: {why}");
                _page.Toast(isDone ? Texts.Format(Texts.Deleted, isChinese, listed.Name) : why, isDone ? ToastKind.Success : ToastKind.Warning);
                Refresh();
            },
            ex =>
            {
                _log($"delete of {listed.FileName} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    /// <summary>
    /// A backup copied into the save folder as a run of its own, under a new file name, then put in the game's list.
    /// The run it came from is not touched, and a deleted save's backups can be copied too.
    /// </summary>
    private void CopyAsNew(string stem, string id)
    {
        var saveFolder = _game.SaveFolder();

        if (saveFolder == null)
        {
            _page.Done();

            return;
        }

        var store = _store;

        _worker.Run(
            () =>
            {
                var backup = store.Find(stem, id);

                if (backup == null) return (Backup: (BackupMeta?)null, NewStem: "", Made: Result.Failed<IReadOnlyList<string>>("no backup " + id));

                var newStem = NewSave.FreeStem(DateTime.Now, name => RunFiles.In(saveFolder, name).Count > 0 || Directory.Exists(store.RunFolder(name)));

                if (newStem == null) return (Backup: backup, NewStem: "", Made: Result.Failed<IReadOnlyList<string>>("no free save name"));

                return (Backup: backup, NewStem: newStem, Made: store.CopyAsNew(stem, id, saveFolder, newStem));
            },
            answer =>
            {
                var isChinese = GameLanguage.IsChinese();

                if (answer.Made is not Result<IReadOnlyList<string>>.Done done || answer.Backup == null)
                {
                    _log($"copy of {stem}/{id} failed: {(answer.Made as Result<IReadOnlyList<string>>.Failed)?.Why}");
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                    Refresh();

                    return;
                }

                var backup = answer.Backup;
                var newFile = RunFiles.MainFile(answer.NewStem);
                var why = _game.MakeOwnRun(saveFolder, newFile, done.Value);

                if (why != null)
                {
                    _log($"copy of {stem}/{id} as {newFile} undone: {why}");
                    DeleteCopied(saveFolder, done.Value);
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                    Refresh();

                    return;
                }

                var name = Texts.Format(Texts.CopyName, isChinese, backup.Run.Name, backup.Run.Progress.Day + 1);

                _game.AddEntry(newFile, name);
                _game.RefreshEntry(newFile);
                _log($"copied {stem}/{id} (day {backup.Run.Progress.Day + 1}) as {newFile}, {done.Value.Count} file(s)");
                _page.Toast(Texts.Format(Texts.Copied, isChinese, name), ToastKind.Success);
                Refresh();
            },
            ex =>
            {
                _log($"copy of {stem}/{id} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    private void DeleteCopied(string saveFolder, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            try
            {
                File.Delete(Path.Combine(saveFolder, name));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log($"could not remove {name} after a failed copy: {ex.Message}");
            }
        }
    }

    private void Export(string stem, string id)
    {
        var store = _store;

        _worker.Run(
            () => store.Export(stem, id, store.ExportsPath),
            made =>
            {
                var isChinese = GameLanguage.IsChinese();

                if (made is Result<string>.Done done)
                {
                    _log("exported " + done.Value);
                    _page.Toast(Texts.Format(Texts.Exported, isChinese, done.Value), ToastKind.Success, 10);
                }
                else
                {
                    _log($"export of {stem}/{id} failed: {(made as Result<string>.Failed)?.Why}");
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                }

                _page.Done();
            },
            ex =>
            {
                _log($"export of {stem}/{id} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                _page.Done();
            });
    }

    private void Import()
    {
        var store = _store;

        _worker.Run(
            () => store.ImportAll(store.ImportPath),
            outcomes =>
            {
                var isChinese = GameLanguage.IsChinese();
                var added = 0;
                var bad = 0;

                foreach (var outcome in outcomes)
                {
                    if (outcome.IsDone) added++;
                    else if (!outcome.IsAlreadyThere) bad++;

                    _log(outcome.IsDone
                        ? $"imported {outcome.File} as {outcome.Meta!.Stem}/{outcome.Meta.Id}"
                        : $"import of {outcome.File}: {outcome.Why}");
                }

                if (outcomes.Count == 0) _page.Toast(Texts.Format(Texts.NothingToImport, isChinese, store.ImportPath), ToastKind.Info, 12);
                else if (bad > 0) _page.Toast(Texts.Format(Texts.ImportedSome, isChinese, added, bad), ToastKind.Warning, 10);
                else _page.Toast(Texts.Format(Texts.Imported, isChinese, added), ToastKind.Success);

                Refresh();
            },
            ex =>
            {
                _log("import failed: " + ex);
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    private void OnDisk(Func<Result<Unit>> step, string what, Phrase? done = null, string arg = "")
    {
        _worker.Run(
            step,
            result =>
            {
                var isChinese = GameLanguage.IsChinese();

                if (result is Result<Unit>.Failed failed)
                {
                    _log($"{what} failed: {failed.Why}");
                    _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
                }
                else if (done is { } phrase)
                {
                    _log(what);
                    _page.Toast(Texts.Format(phrase, isChinese, RunName(arg)), ToastKind.Success);
                }

                Refresh();
            },
            ex =>
            {
                _log($"{what} failed: {ex}");
                Refresh();
            });
    }

    /// <summary>The run's name in the game's list, else the name its newest backup carries, else the file name.</summary>
    private string RunName(string stem)
    {
        if (stem.Length == 0) return "";
        if (Listed(stem) is { } run && run.Name.Length > 0) return run.Name;

        try
        {
            return _store.Of(stem).OrderByDescending(meta => meta.CreatedUtc).FirstOrDefault()?.Run.Name is { Length: > 0 } name ? name : stem;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return stem;
        }
    }

    private void EmptyBin()
    {
        var store = _store;

        _worker.Run(
            () => store.EmptyBin(),
            count =>
            {
                _log($"emptied the bin, {count} backup(s) deleted for good");
                _page.Toast(Texts.Format(Texts.BinEmptied, GameLanguage.IsChinese(), count), ToastKind.Success);
                Refresh();
            },
            ex =>
            {
                _log("emptying the bin failed: " + ex);
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    /// <summary>A setting from the Settings tab, written to the cfg file; the mod picks it up as if the file had been edited.</summary>
    private void SetSetting(string id, string value)
    {
        var why = _setSetting(id, value);

        if (why == null)
        {
            _log($"setting {id} = {value}");
        }
        else
        {
            _log($"setting {id} = {value} refused: {why}");
            _page.Toast(Texts.Format(Texts.SettingRefused, GameLanguage.IsChinese(), why), ToastKind.Warning);
        }

        Refresh();
    }

    private void Rename(string stem, string name)
    {
        var isDone = _game.Rename(RunFiles.MainFile(stem), name);

        _log(isDone ? $"renamed {stem} to {name}" : $"rename of {stem}: it is not in the save list");

        if (isDone) _page.Toast(Texts.Format(Texts.Renamed, GameLanguage.IsChinese(), name), ToastKind.Success);
        else _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);

        Refresh();
    }

    /// <summary>The run's save as it is on disk now, copied as a new save in the game's list.</summary>
    private void Duplicate(string stem)
    {
        var saveFolder = _game.SaveFolder();
        var listed = Listed(stem);

        if (saveFolder == null || listed == null)
        {
            _page.Done();

            return;
        }

        // The run being played is saved first, so the copy is where the player is.
        if (string.Equals(GameSaves.CurrentFile(), listed.FileName, StringComparison.OrdinalIgnoreCase))
        {
            if (_game.IsSafeToSave()) _game.SaveNow();
            else GameSaves.Flush();
        }

        var store = _store;
        var name = Texts.Format(Texts.DuplicateName, GameLanguage.IsChinese(), listed.Name);

        _worker.Run(
            () =>
            {
                var newStem = FreeStem(saveFolder, store);

                if (newStem == null) return (NewStem: "", Made: Result.Failed<IReadOnlyList<string>>("no free save name"));

                var files = RunFiles.In(saveFolder, stem).Where(file => NewSave.IsCopied(stem, file)).ToList();
                var targets = files.Select(file => NewSave.Rename(file, stem, newStem)).ToList();

                try
                {
                    for (var i = 0; i < files.Count; i++) File.Copy(Path.Combine(saveFolder, files[i]), Path.Combine(saveFolder, targets[i]));

                    return (NewStem: newStem, Made: Result.Done<IReadOnlyList<string>>(targets));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    foreach (var target in targets)
                    {
                        try
                        {
                            File.Delete(Path.Combine(saveFolder, target));
                        }
                        catch (Exception ignored) when (ignored is IOException or UnauthorizedAccessException)
                        {
                            // The next copy finds the name taken and picks another.
                        }
                    }

                    return (NewStem: "", Made: Result.Failed<IReadOnlyList<string>>(ex.Message));
                }
            },
            answer => Copied(saveFolder, stem, answer.NewStem, answer.Made, name, Texts.Copied),
            ex =>
            {
                _log($"copy of {stem} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    private static string? FreeStem(string saveFolder, BackupStore store) =>
        NewSave.FreeStem(DateTime.Now, taken => RunFiles.In(saveFolder, taken).Count > 0 || Directory.Exists(store.RunFolder(taken)));

    /// <summary>After files were copied in as a new save: names it inside, puts it in the game's list, or undoes the copy. An empty name takes the one inside the save.</summary>
    private void Copied(string saveFolder, string from, string newStem, Result<IReadOnlyList<string>> made, string name, Phrase toast)
    {
        var isChinese = GameLanguage.IsChinese();

        if (made is not Result<IReadOnlyList<string>>.Done done)
        {
            _log($"copy of {from} failed: {(made as Result<IReadOnlyList<string>>.Failed)?.Why}");
            _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
            Refresh();

            return;
        }

        var newFile = RunFiles.MainFile(newStem);
        var why = _game.MakeOwnRun(saveFolder, newFile, done.Value);

        if (why != null)
        {
            _log($"copy of {from} as {newFile} undone: {why}");
            DeleteCopied(saveFolder, done.Value);
            _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
            Refresh();

            return;
        }

        if (name.Length == 0) name = _game.SaveName(newFile) is { Length: > 0 } inside ? inside : newStem;

        _game.AddEntry(newFile, name);
        _game.RefreshEntry(newFile);
        _log($"copied {from} as {newFile} ({name}), {done.Value.Count} file(s)");
        _page.Toast(Texts.Format(toast, isChinese, name), ToastKind.Success);
        Refresh();
    }

    /// <summary>
    /// What the detail view needs that the list does not carry. Backups made before stats were kept are read here a
    /// few at a time, and their backup.json written again with the stats.
    /// </summary>
    private void Detail(string stem)
    {
        var store = _store;

        _worker.Run(
            () => store.Of(stem),
            backups =>
            {
                if (!_isOpen) return;

                var saveFolder = _game.SaveFolder();
                var filled = new List<BackupMeta>();
                var shown = new List<BackupMeta>();

                foreach (var meta in backups)
                {
                    if (meta.Stats == null && filled.Count < StatsReadsPerDetail
                        && SaveReader.OfFile(Path.Combine(store.RunFolder(stem), meta.Id, RunFiles.MainFile(stem))) is { } read)
                    {
                        var withStats = meta with { Stats = read };

                        filled.Add(withStats);
                        shown.Add(withStats);
                    }
                    else
                    {
                        shown.Add(meta);
                    }
                }

                var isPlaying = string.Equals(GameSaves.CurrentFile(), RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase);
                var now = isPlaying ? SaveReader.Current() : null;
                var listed = Listed(stem);

                if (now == null && saveFolder != null && listed != null) now = SaveReader.OfFile(Path.Combine(saveFolder, RunFiles.MainFile(stem)));

                var snaps = _settings.ShowGameSnapshots && saveFolder != null && listed != null
                    ? SaveReader.Snaps(saveFolder, stem, listed.Progress.Turn)
                    : new List<GameSnap>();
                var ids = new HashSet<int>();

                foreach (var stats in shown.Select(meta => meta.Stats).Append(now).Concat(snaps.Select(snap => snap.Stats)))
                    if (stats != null) ids.UnionWith(stats.Items.Keys);

                _page.Detail(PageModel.Detail(new RunDetail(stem, now, shown, snaps, SaveReader.Photos(stem), SaveReader.ItemNames(ids))));

                if (filled.Count == 0) return;

                _worker.Run(
                    () => filled.Count(meta => store.Rewrite(meta) is Result<Unit>.Done),
                    count => _log($"read the stats of {count} older backup(s) of {stem}"),
                    ex => _log($"could not keep the stats of {stem}'s backups: {ex.Message}"));
            },
            ex =>
            {
                _log($"detail of {stem} failed: {ex}");
                _page.Done();
            });
    }

    /// <summary>One of the game's own snapshots of the run, written as a new save in the game's list.</summary>
    private void SnapshotAsNew(string stem, string file)
    {
        var saveFolder = _game.SaveFolder();
        var listed = Listed(stem);
        var isChinese = GameLanguage.IsChinese();

        if (saveFolder == null || listed == null || !RunFiles.Belongs(stem, file))
        {
            _page.Done();

            return;
        }

        var newStem = FreeStem(saveFolder, _store);

        if (newStem == null)
        {
            _log($"snapshot {file} as a new save: no free save name");
            _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
            _page.Done();

            return;
        }

        var newFile = RunFiles.MainFile(newStem);
        var why = SaveReader.SnapshotAsNew(saveFolder, stem, file, newFile, out var day);

        if (why != null)
        {
            _log($"snapshot {file} as a new save failed: {why}");
            DeleteCopied(saveFolder, new[] { newFile });
            _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
            Refresh();

            return;
        }

        var name = Texts.Format(Texts.CopyName, isChinese, listed.Name, day);

        _game.AddEntry(newFile, name);
        _game.RefreshEntry(newFile);
        _log($"snapshot {file} (day {day}) is now {newFile}");
        _page.Toast(Texts.Format(Texts.SnapCopied, isChinese, day, name), ToastKind.Success);
        Refresh();
    }

    /// <summary>The run's save as it is now as a .slsave, with all its backups when asked.</summary>
    private void ExportRun(string stem, bool isAll, bool withExtras)
    {
        var saveFolder = _game.SaveFolder();
        var listed = Listed(stem);
        var store = _store;
        var local = DateTime.Now;
        var stamp = local.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var stats = listed != null && saveFolder != null ? SaveReader.OfFile(Path.Combine(saveFolder, listed.FileName)) : null;
        var now = listed == null || saveFolder == null
            ? null
            : new BackupMeta(stamp + "-manual", stem, BackupKind.Manual, DateTime.UtcNow, listed, 0, _game.Version(), IsPinned: false) { Stats = stats };

        _worker.Run(
            () =>
            {
                var ids = isAll || now == null ? store.Of(stem).Select(meta => meta.Id).ToList() : new List<string>();

                return store.Export(stem, new ExportPick(saveFolder, now, ids, withExtras), store.ExportsPath, stem + "-" + stamp);
            },
            made => Exported(stem, made),
            ex => Exported(stem, Result.Failed<string>(ex.ToString())));
    }

    private void Exported(string what, Result<string> made)
    {
        var isChinese = GameLanguage.IsChinese();

        if (made is Result<string>.Done done)
        {
            _log("exported " + done.Value);
            _page.Toast(Texts.Format(Texts.Exported, isChinese, done.Value), ToastKind.Success, 10);
        }
        else
        {
            _log($"export of {what} failed: {(made as Result<string>.Failed)?.Why}");
            _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
        }

        _page.Done();
    }

    private void ImportList()
    {
        var store = _store;

        _worker.Run(
            () =>
            {
                Directory.CreateDirectory(store.ImportPath);

                return PageModel.Imports(store.ImportCandidates(), store.ImportPath);
            },
            json => _page.Imports(json),
            ex =>
            {
                _log("could not list the Import folder: " + ex);
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                _page.Done();
            });
    }

    /// <summary>One file from the Import folder: a .slsave into the backups, or a loose save file in as a new save.</summary>
    private void ImportFile(string file)
    {
        var store = _store;

        if (!BackupStore.IsLooseSave(file))
        {
            _worker.Run(
                () => store.ImportFile(file),
                outcome =>
                {
                    var isChinese = GameLanguage.IsChinese();

                    _log(outcome.IsDone ? $"imported {file} as {outcome.Meta!.Stem}, {outcome.Count} backup(s)" : $"import of {file}: {outcome.Why}");

                    if (outcome.IsDone || outcome.IsAlreadyThere) _page.Toast(Texts.Format(Texts.Imported, isChinese, outcome.Count), ToastKind.Success);
                    else _page.Toast(Texts.Format(Texts.ImportedSome, isChinese, 0, 1), ToastKind.Warning, 10);

                    Refresh();
                    ImportList();
                },
                ex =>
                {
                    _log($"import of {file} failed: {ex}");
                    _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                    Refresh();
                });

            return;
        }

        var saveFolder = _game.SaveFolder();

        if (saveFolder == null)
        {
            _page.Done();

            return;
        }

        _worker.Run(
            () => FreeStem(saveFolder, store) is { } newStem
                ? (NewStem: newStem, Made: store.CopyLoose(file, saveFolder, newStem))
                : (NewStem: "", Made: Result.Failed<IReadOnlyList<string>>("no free save name")),
            answer =>
            {
                Copied(saveFolder, file, answer.NewStem, answer.Made, "", Texts.ImportedSave);
                ImportList();
            },
            ex =>
            {
                _log($"import of {file} failed: {ex}");
                _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                Refresh();
            });
    }

    private void OpenFolder(string stem)
    {
        try
        {
            var folder = stem.Length > 0 && Directory.Exists(_store.RunFolder(stem)) ? _store.RunFolder(stem) : _settings.Root;

            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log("could not open the backup folder: " + ex.Message);
        }

        _page.Done();
    }
}
