using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ExiledToolkit;
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

    private readonly GameSaves _game;
    private readonly WebPage _page;
    private readonly DiskWorker _worker;
    private readonly Action<string> _log;
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

    public Session(GameSaves game, WebPage page, DiskWorker worker, Settings settings, Action<string> log)
    {
        _game = game;
        _page = page;
        _worker = worker;
        _settings = settings;
        _log = log;
        _store = new BackupStore(settings.Root, () => DateTime.UtcNow);
    }

    public void UseSettings(Settings settings)
    {
        if (!string.Equals(settings.Root, _settings.Root, StringComparison.Ordinal)) _store = new BackupStore(settings.Root, () => DateTime.UtcNow);

        _settings = settings;
        _log($"backups in {settings.Root}; morning {settings.OnMorning}, quit {settings.OnQuit}, rebirth {settings.BeforeRebirth}, expedition {settings.BeforeExpedition}, wave {settings.BeforeWave}, ending {settings.AtEnding}, load screen {settings.ReplaceLoadScreen}");
    }

    public void UseKey(KeyCode key) => _backupKey = key;

    /// <summary>Clears what a crash left half-written, before any backup is made.</summary>
    public void Start()
    {
        var store = _store;

        _worker.Run(() =>
        {
            Directory.CreateDirectory(store.ImportPath);

            return store.SweepPartials();
        }, swept =>
        {
            if (swept > 0) _log($"removed {swept} unfinished backup(s) left by a crash");
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

        if (_isTitleCheckDue && _page.IsReady) CheckTitle();
        if (!Input.anyKeyDown) return;
        if (_isOpen && !_keyboard.IsClaimed && Input.GetKeyDown(KeyCode.Escape)) _page.Escape();
        // A key typed into a text box, here or in another mod's window, is text, not a backup.
        if (KeyGate.Fires(_backupKey, KeyCode.None, IsKeyDown, OptionalLive.IsTyping) && _game.CurrentRun() != null) BackUpRunning(BackupKind.Manual, isAsked: true);
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

    private void Close()
    {
        _keyboard.Release();
        _isOpen = false;
        HotkeyGate.MenuClosed();
        _page.Close();
    }

    /// <summary>Reads the game's list here, then the backups on the worker, and sends the page the whole view.</summary>
    private void Refresh(bool isOpening = false)
    {
        if (!_isOpen) return;

        var state = _mainState;
        var runs = state == null ? new List<RunView>() : _game.LoadScreenRuns(state);
        var store = _store;
        var key = _backupKey == KeyCode.None ? "" : _backupKey.ToString();
        var isChinese = GameLanguage.IsChinese();
        var version = _game.Version();

        _worker.Run(
            () => PageModel.Json(new Core.Screen(runs, store.All(), store.AllSettled(), store.Root, key, isChinese, version)),
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

    /// <summary>Copies the run's files on the worker, then trims the backups to the limits.</summary>
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

        _worker.Run(
            () =>
            {
                if (Repeats.IsRepeat(kind, facts.Progress, store.Of(stem), DateTime.UtcNow)) return null;

                var made = store.Create(saveFolder, facts, kind, version, dayText);

                if (made is Result<BackupMeta>.Done done)
                {
                    if (settleAfter) store.Settle(stem, done.Value.CreatedUtc);

                    store.Prune(limits);
                }

                return made;
            },
            made => Backed(facts, kind, isAsked, made),
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
                _log($"{BackupKinds.Id(kind)} backup {done.Value.Stem}/{done.Value.Id}, day {facts.Progress.Day + 1}, {done.Value.Bytes / 1024} KB");

                if (isAsked || _settings.AutoBackupToasts) _page.Toast(Texts.Format(Texts.BackedUp, isChinese, facts.Name, facts.Progress.Day + 1), ToastKind.Success);

                break;
            case Result<BackupMeta>.Failed failed:
                _log($"{BackupKinds.Id(kind)} backup of {facts.FileName} failed: {failed.Why}");
                _page.Toast(Texts.Failed.Pick(isChinese), ToastKind.Error);
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
            case PageCommand.OpenFolder:
                OpenFolder();
                break;
            case PageCommand.BackUp backUp:
                BackUpListed(backUp.Stem);
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

    private void OnDisk(Func<Result<Unit>> step, string what)
    {
        _worker.Run(
            step,
            done =>
            {
                if (done is Result<Unit>.Failed failed)
                {
                    _log($"{what} failed: {failed.Why}");
                    _page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()), ToastKind.Error);
                }

                Refresh();
            },
            ex =>
            {
                _log($"{what} failed: {ex}");
                Refresh();
            });
    }

    private void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(_settings.Root);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + _settings.Root + "\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log("could not open the backup folder: " + ex.Message);
        }

        _page.Done();
    }
}
