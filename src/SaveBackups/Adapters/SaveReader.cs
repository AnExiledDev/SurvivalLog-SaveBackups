using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameCore.HotUpdate;
using SaveBackups.Core;

namespace SaveBackups.Adapters;

/// <summary>
/// Reads what is inside saves with the game's own reader: the survivor's meters, money, points and items, the game's
/// day and round snapshots, and the game's tables for survivor pictures and item names. All of it on the main thread;
/// every read fails soft to null or an empty list.
/// </summary>
internal static class SaveReader
{
    // Config_Attr ids: the value and its most.
    private const int Fullness = 1;
    private const int Mood = 2;
    private const int Stamina = 3;
    private const int Health = 4;
    private const int MaxOffset = 100;

    private const string SnapMark = ".snap";
    private const string RoundFile = ".round.bytes";

    /// <summary>The run being played, from the save the game holds in memory.</summary>
    public static SaveStats? Current()
    {
        try
        {
            var manager = GameSingleton.Existing<GameSaveManager>();

            return manager != null && manager.IsInBattle ? Of(manager.GetGameSave()?.CurSave) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>A save's main file anywhere on disk (in a backup, or the save folder).</summary>
    public static SaveStats? OfFile(string path)
    {
        try
        {
            return File.Exists(path) ? Of(Read(path)?.CurSave) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static GameSaveData? Read(string path) => MemoryPackToolsExpand.DeserializeObject<GameSaveData>(TemporaryFile.ReadFileBytes(path));

    public static SaveStats? Of(SaveChildData? child)
    {
        var lead = child?.LeadingRole;

        if (child == null || lead == null) return null;

        var items = new Dictionary<int, int>();

        Count(lead.ItemList, items);

        var map = child.ChapterAgentMap;

        if (map != null)
        {
            foreach (var agents in map.Values)
            {
                if (agents == null) continue;

                for (var i = 0; i < agents.Count; i++)
                {
                    var agent = agents[i];

                    // Shops carry stock that is not the player's.
                    if (agent != null && agent.ShopConfigId == 0) Count(agent.ItemList, items);
                }
            }
        }

        var attrs = lead.AttrDict;

        return new SaveStats(
            Meter(attrs, Health),
            Meter(attrs, Fullness),
            Meter(attrs, Mood),
            Meter(attrs, Stamina),
            lead.Money,
            child.SurvivalPoint,
            items,
            Journal(child));
    }

    /// <summary>The game's own day and round snapshots of a run, oldest day first, the round snapshot last.</summary>
    public static List<GameSnap> Snaps(string saveFolder, string stem, int turn)
    {
        var snaps = new List<GameSnap>();

        foreach (var name in RunFiles.In(saveFolder, stem))
        {
            var path = Path.Combine(saveFolder, name);

            try
            {
                var info = new FileInfo(path);

                if (name.EndsWith(RoundFile, StringComparison.OrdinalIgnoreCase))
                {
                    var round = MemoryPackToolsExpand.DeserializeObject<RebirthRoundSnapshot>(TemporaryFile.ReadFileBytes(path));
                    var child = round?.Save?.CurSave;

                    if (child == null) continue;

                    snaps.Add(new GameSnap(name, true, new Progress(round!.TurnIndex + 1, child.Day, child.GameTime), info.Length, info.LastWriteTimeUtc, Of(child)));
                }
                else if (name.Contains(SnapMark, StringComparison.OrdinalIgnoreCase))
                {
                    var day = MemoryPackToolsExpand.DeserializeObject<RebirthDaySnapshot>(TemporaryFile.ReadFileBytes(path));
                    var child = day?.Child;

                    if (child == null) continue;

                    snaps.Add(new GameSnap(name, false, new Progress(turn, child.Day, child.GameTime), info.Length, info.LastWriteTimeUtc, Of(child)));
                }
            }
            catch (Exception)
            {
                // A snapshot the game itself could not read is not offered.
            }
        }

        return snaps.OrderBy(snap => snap.IsRound).ThenBy(snap => snap.Progress).ToList();
    }

    /// <summary>
    /// Turns one of the run's snapshots into the save <paramref name="newFile"/>, written next to it. A day snapshot
    /// holds only the day; it goes in place of the run's day, and the run's points and faith stay as they are now, as
    /// the game's own rebirth does. A round snapshot holds the whole save. Gives null when it worked, or why not.
    /// </summary>
    public static string? SnapshotAsNew(string saveFolder, string stem, string snapFile, string newFile, out int day)
    {
        day = 0;

        try
        {
            var mainPath = Path.Combine(saveFolder, RunFiles.MainFile(stem));
            var snapPath = Path.Combine(saveFolder, snapFile);

            if (!File.Exists(mainPath) || !File.Exists(snapPath)) return "the save or the snapshot is gone";

            GameSaveData? save;

            if (snapFile.EndsWith(RoundFile, StringComparison.OrdinalIgnoreCase))
            {
                save = MemoryPackToolsExpand.DeserializeObject<RebirthRoundSnapshot>(TemporaryFile.ReadFileBytes(snapPath))?.Save;
            }
            else
            {
                save = Read(mainPath);

                var child = MemoryPackToolsExpand.DeserializeObject<RebirthDaySnapshot>(TemporaryFile.ReadFileBytes(snapPath))?.Child;
                var now = save?.CurSave;

                if (save == null || child == null || now == null) return "the snapshot could not be read";

                child.SurvivalTotalPoints = now.SurvivalTotalPoints;
                child.IsFaithSettled = now.IsFaithSettled;
                child.FaithSettledTotal = now.FaithSettledTotal;
                child.FaithSettledStages = now.FaithSettledStages;
                child.PendingDaySettlement = false;
                child.PendingDeathChoice = false;
                child.PendingEndingChoiceEventId = 0;
                save.CurSave = child;
            }

            if (save == null) return "the snapshot could not be read";

            save.FileName = newFile;
            day = (save.CurSave?.Day ?? 0) + 1;
            TemporaryFile.WritFile(TemporaryFile.GetFilePath(newFile), MemoryPackToolsExpand.SerializeObject<GameSaveData>(save), false);
            GameSaves.Flush();

            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Every survivor the new game screen offers, by PlayerSelect id.</summary>
    public static Dictionary<int, Survivor> Survivors()
    {
        var survivors = new Dictionary<int, Survivor>();

        try
        {
            var config = GameSingleton.Existing<ConfigManager>();
            var table = config?._Config_PlayerSelect_Dict;

            if (config == null || table == null) return survivors;

            foreach (var row in table.Values)
            {
                if (row == null) continue;

                survivors[row.ID] = new Survivor(
                    Text(config, row.Name, LanguageType.English) ?? "",
                    Text(config, row.Name, LanguageType.Chinese) ?? NonEmpty(row.Name_Local) ?? "",
                    row.NpcPic ?? "",
                    row.BgPic ?? "");
            }
        }
        catch (Exception)
        {
            // No pictures; the page draws the survivor's initial.
        }

        return survivors;
    }

    public static Dictionary<int, Phrase> ItemNames(IEnumerable<int> ids)
    {
        var names = new Dictionary<int, Phrase>();
        var config = GameSingleton.Existing<ConfigManager>();
        var table = config?._Config_Item_Dict;

        if (config == null || table == null) return names;

        foreach (var id in ids)
        {
            try
            {
                if (names.ContainsKey(id) || !table.ContainsKey(id)) continue;

                var row = table[id];

                if (row == null) continue;

                var en = Text(config, row.ItemName, LanguageType.English) ?? "#" + id;

                names[id] = new Phrase(en, Text(config, row.ItemName, LanguageType.Chinese) ?? NonEmpty(row.ItemName_Local) ?? en);
            }
            catch (Exception)
            {
                // The page shows the id.
            }
        }

        return names;
    }

    /// <summary>The ending photos the game took of the run, as file:// URLs.</summary>
    public static List<string> Photos(string stem)
    {
        try
        {
            var folder = EndingPhotoStore.GetDir();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return new List<string>();

            // The game names them after the main file: <stem>.bytes.ending.<id>.jpg, with a slot before .jpg past the first.
            return Directory.EnumerateFiles(folder, stem + ".*.jpg")
                .Where(path => Path.GetFileName(path).StartsWith(RunFiles.MainFile(stem) + ".ending.", StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(path).StartsWith(stem + ".ending.", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => new Uri(path).AbsoluteUri)
                .ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    private static void Count(Il2CppSystem.Collections.Generic.List<ItemSave>? list, Dictionary<int, int> items)
    {
        if (list == null) return;

        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];

            if (item == null || item.ItemConfigId <= 0 || item.ItemCount <= 0) continue;

            items[item.ItemConfigId] = items.TryGetValue(item.ItemConfigId, out var count) ? count + item.ItemCount : item.ItemCount;
        }
    }

    private static Meter Meter(Il2CppSystem.Collections.Generic.Dictionary<int, int>? attrs, int id)
    {
        if (attrs == null) return default;

        var value = attrs.ContainsKey(id) ? attrs[id] : 0;
        var max = attrs.ContainsKey(id + MaxOffset) ? attrs[id + MaxOffset] : 0;

        return new Meter(value, max);
    }

    private static string Journal(SaveChildData child)
    {
        try
        {
            var text = GameSaveManager.GetDayDescription(child, child.Day);

            return string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
        }
        catch (Exception)
        {
            return "";
        }
    }

    /// <summary>The game answers a key it has no text for with the key itself.</summary>
    private static string? Text(ConfigManager config, string? key, LanguageType language)
    {
        if (string.IsNullOrEmpty(key)) return null;

        try
        {
            var text = config.GetLocalTxtByLanguage(key, language);

            return string.IsNullOrWhiteSpace(text) || text == key ? null : text.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
