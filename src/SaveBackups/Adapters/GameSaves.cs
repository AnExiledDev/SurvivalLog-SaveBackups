using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using SaveBackups.Core;

namespace SaveBackups.Adapters;

/// <summary>
/// Everything the mod asks the game about saves, on the main thread. Several of these are private in the game and
/// public in the interop assemblies. Every call fails soft: a null or false answer, and the caller says so.
/// </summary>
internal sealed class GameSaves
{
    private const string Caller = "SaveBackups";

    private static GameSaveManager? Manager => GameSingleton.Existing<GameSaveManager>();

    /// <summary>The game's save folder (LocalLow/LLS/SLGame/Saves), or null before the game has made its save manager.</summary>
    public string? SaveFolder()
    {
        if (Manager == null) return null;

        var root = TemporaryFile.GetRootPath();

        return string.IsNullOrEmpty(root) ? null : root;
    }

    public string Version()
    {
        try
        {
            return Manager?.GetCurrentVersion() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    /// <summary>The run being played, as its save list entry would show it, or null at the title screen.</summary>
    public RunFacts? CurrentRun()
    {
        var manager = Manager;

        if (manager == null || !manager.IsInBattle) return null;

        var save = manager.GetGameSave();

        if (save == null || string.IsNullOrEmpty(save.FileName)) return null;

        var child = new HistoryChild();

        GameSaveManager.AssignChildMetaFromGameSave(child, save);

        // A save copied by the mod keeps the old run's name inside it; its save list entry has the copy's name.
        return Facts(save.FileName, ListedName(save.FileName) ?? save.Name ?? "", child);
    }

    private string? ListedName(string fileName)
    {
        var list = Manager?.GetHistoryList();

        if (list == null) return null;

        for (var i = 0; i < list.Count; i++)
        {
            var child = list[i];

            if (child != null && string.Equals(child.FileName, fileName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(child.Name)) return child.Name;
        }

        return null;
    }

    /// <summary>The ending event the run has chosen and saved, or 0 when none (or no run).</summary>
    public static int PendingEndingId()
    {
        var manager = Manager;

        return manager != null && manager.IsInBattle ? manager.GetGameSave()?.CurSave?.PendingEndingChoiceEventId ?? 0 : 0;
    }

    /// <summary>The game's own journal line for the day the run is on, or "" when it has none.</summary>
    public string DayText()
    {
        try
        {
            var manager = Manager;
            var save = manager != null && manager.IsInBattle ? manager.GetGameSave() : null;
            var child = save?.CurSave;

            if (child == null) return "";

            var text = GameSaveManager.GetDayDescription(child, child.Day);

            return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
        }
        catch (Exception)
        {
            return "";
        }
    }

    /// <summary>
    /// Makes files copied from a backup into a run of their own. Each file names its run inside it, and the game
    /// saves to the name it reads there, so a copy left as it is would save over the run it came from. The main
    /// file and the snapshots are read with the game's own reader, given the new name and written back. A snapshot
    /// that cannot be rewritten is deleted (the game would refuse it anyway). Gives null when it worked, or why not.
    /// </summary>
    public string? MakeOwnRun(string saveFolder, string newFile, IReadOnlyList<string> copied)
    {
        var manager = Manager;

        if (manager == null) return "the save manager is not loaded";

        var stem = RunFiles.Stem(newFile);

        try
        {
            var save = manager.PeekGameSave(newFile);

            if (save == null) return "the game could not read the copy";

            save.FileName = newFile;
            TemporaryFile.WritFile(TemporaryFile.GetFilePath(newFile), MemoryPackToolsExpand.SerializeObject<GameSaveData>(save), false);
        }
        catch (Exception ex)
        {
            return "the copy could not be renamed inside: " + ex.Message;
        }

        foreach (var name in copied)
        {
            if (string.Equals(name, RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)) continue;

            var path = System.IO.Path.Combine(saveFolder, name);

            try
            {
                if (name.EndsWith(".round.bytes", StringComparison.OrdinalIgnoreCase))
                {
                    var round = MemoryPackToolsExpand.DeserializeObject<RebirthRoundSnapshot>(TemporaryFile.ReadFileBytes(path));

                    if (round == null) throw new InvalidOperationException("empty");

                    round.SaveFileName = newFile;

                    if (round.Save != null) round.Save.FileName = newFile;

                    TemporaryFile.WritFile(path, MemoryPackToolsExpand.SerializeObject<RebirthRoundSnapshot>(round), false);
                }
                else if (name.Contains(".snap", StringComparison.OrdinalIgnoreCase))
                {
                    var day = MemoryPackToolsExpand.DeserializeObject<RebirthDaySnapshot>(TemporaryFile.ReadFileBytes(path));

                    if (day == null) throw new InvalidOperationException("empty");

                    day.SaveFileName = newFile;
                    TemporaryFile.WritFile(path, MemoryPackToolsExpand.SerializeObject<RebirthDaySnapshot>(day), false);
                }
                else
                {
                    // A file this mod does not know the inside of; without a rewrite it would point at the old run.
                    TemporaryFile.DeleteFile(path);
                }
            }
            catch (Exception)
            {
                TemporaryFile.DeleteFile(path);
            }
        }

        Flush();

        try
        {
            var check = manager.PeekGameSave(newFile);

            return check != null && string.Equals(check.FileName, newFile, StringComparison.OrdinalIgnoreCase) ? null : "the copy still names the old save inside";
        }
        catch (Exception ex)
        {
            return "the copy could not be read back: " + ex.Message;
        }
    }

    /// <summary>Whether the game itself would save now (not mid-load, not between worlds).</summary>
    public bool IsSafeToSave() => Manager?.IsWorldSafeToPersist(Caller) == true;

    /// <summary>Saves the run being played and waits for the file to be on disk.</summary>
    public void SaveNow()
    {
        Manager?.SaveGame();
        Flush();
    }

    /// <summary>Writes out any save the game queued, so the files on disk are the ones a backup should copy.</summary>
    public static void Flush() => TemporaryFile.FlushPendingWrites();

    /// <summary>The runs in the game's save list, as facts.</summary>
    /// <summary>False until the game has read its save history; until then its history calls throw.</summary>
    public bool HasHistory() => Manager?.historyData != null;

    public List<RunFacts> History()
    {
        var runs = new List<RunFacts>();
        var list = HasHistory() ? Manager!.GetHistoryList() : null;

        if (list == null) return runs;

        for (var i = 0; i < list.Count; i++)
        {
            var child = list[i];

            if (child != null && !string.IsNullOrEmpty(child.FileName)) runs.Add(Facts(child.FileName, child.Name ?? "", child));
        }

        return runs;
    }

    public bool HasSaveList() => HasHistory() && Manager!.HasSaveList();

    /// <summary>
    /// The runs the game's Load screen would list, with its own texts, read from the list it just built for
    /// <paramref name="state"/>. Progress comes from the save list entry with the same file name.
    /// </summary>
    public List<RunView> LoadScreenRuns(State_Web_MainUI state)
    {
        var views = new List<RunView>();

        Reducer_Web_MainUI.RebuildSaveList(state);

        var list = state.ListView_Save;

        if (list == null) return views;

        var facts = new Dictionary<string, RunFacts>(StringComparer.OrdinalIgnoreCase);

        foreach (var run in History()) facts[run.FileName] = run;

        for (var i = 0; i < list.Count; i++)
        {
            var row = list[i];

            if (row == null || string.IsNullOrEmpty(row.FileName)) continue;

            var run = facts.TryGetValue(row.FileName, out var found) ? found : null;

            views.Add(new RunView(
                row.FileName,
                run?.Name ?? "",
                run?.Progress ?? default,
                Text(row.RoleText),
                Text(row.TimeText),
                Text(row.TurnText),
                Text(row.StatusText),
                Text(row.DiffText),
                Text(row.PhaseText),
                Flag(row.IsFinished),
                Flag(row.IsEndless)));
        }

        return views;
    }

    /// <summary>After a restore the save list entry still shows the old day; it is read again from the restored file and written out.</summary>
    public void RefreshEntry(string fileName)
    {
        var manager = Manager;
        var list = manager?.GetHistoryList();

        if (manager == null || list == null) return;

        for (var i = 0; i < list.Count; i++)
        {
            var child = list[i];

            if (child == null || !string.Equals(child.FileName, fileName, StringComparison.OrdinalIgnoreCase)) continue;

            var save = manager.PeekGameSave(fileName);

            if (save != null) GameSaveManager.AssignChildMetaFromGameSave(child, save);

            manager.PersistHistoryData();

            return;
        }
    }

    /// <summary>The game's own delete; the reason is the game's words when it refuses.</summary>
    public (bool IsDone, string Why) Delete(string fileName)
    {
        var manager = Manager;

        if (manager == null) return (false, "the save manager is not loaded");

        var answer = manager.DeleteGameSave(fileName);

        return (answer.Item1, answer.Item2 ?? "");
    }

    /// <summary>Puts a run whose files are back on disk into the game's save list.</summary>
    public bool AddEntry(string fileName, string name)
    {
        var manager = Manager;

        if (manager == null) return false;

        manager.AddHistoryEntry(fileName, name);

        return true;
    }

    public bool IsListed(string fileName)
    {
        foreach (var run in History())
            if (string.Equals(run.FileName, fileName, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>Starts the run, as the game's own Load button does.</summary>
    public static void Enter(string fileName) => Reducer_Web_MainUI.EnterSaveByFileName(fileName);

    private static RunFacts Facts(string fileName, string name, HistoryChild child) =>
        new(fileName, name, new Progress(child.Turn, child.Day, child.GameTime), child.PlayerSelectId, child.DifficultyPresetId, child.IsFinished);

    private static string Text(R3.ReactiveProperty<string>? property) => property?.Value ?? "";

    private static bool Flag(R3.ReactiveProperty<bool>? property) => property?.Value == true;
}
