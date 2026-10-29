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

        return Facts(save.FileName, save.Name ?? "", child);
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
    public List<RunFacts> History()
    {
        var runs = new List<RunFacts>();
        var list = Manager?.GetHistoryList();

        if (list == null) return runs;

        for (var i = 0; i < list.Count; i++)
        {
            var child = list[i];

            if (child != null && !string.IsNullOrEmpty(child.FileName)) runs.Add(Facts(child.FileName, child.Name ?? "", child));
        }

        return runs;
    }

    public bool HasSaveList() => Manager?.HasSaveList() == true;

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
