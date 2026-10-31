using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>A run in the game's save list, with the texts the game's own Load screen shows for it, read on the main thread.</summary>
public sealed record RunView(
    string FileName,
    string Name,
    Progress Progress,
    string Role,
    string Time,
    string Turn,
    string Status,
    string Difficulty,
    string Phase,
    bool IsFinished,
    bool IsEndless)
{
    /// <summary>The survivor's PlayerSelect id, for their portrait; 0 when unknown.</summary>
    public int SurvivorId { get; init; }
}

/// <summary>A survivor as the new game screen shows them: names in both languages and the two pictures under the WebUI's UI/SelectPeople.</summary>
public sealed record Survivor(string NameEn, string NameZh, string Portrait, string Background);

/// <summary>A run's save files on disk: their size together and when the main file was last written.</summary>
public readonly record struct SaveDisk(long Bytes, DateTime WrittenUtc);

/// <summary>Everything the save manager draws from: the game's runs, the backups on disk and each run's settled time.</summary>
public sealed record Screen(
    IReadOnlyList<RunView> Runs,
    IReadOnlyList<BackupMeta> Backups,
    IReadOnlyDictionary<string, DateTime> Settled,
    string Folder,
    string BackupKey,
    bool IsChinese,
    string GameVersion = "",
    long LimitBytes = 0)
{
    /// <summary>What the mod keeps about each run (star, note, play time, when it left the list), by stem.</summary>
    public IReadOnlyDictionary<string, RunNote> Notes { get; init; } = new Dictionary<string, RunNote>();

    public IReadOnlyList<BinEntry> Bin { get; init; } = Array.Empty<BinEntry>();

    /// <summary>How many days the bin keeps things; 0 keeps them until emptied.</summary>
    public int BinDays { get; init; }

    /// <summary>The backup folder as a file:// URL ending in "/", so the page can show the pictures in it.</summary>
    public string ShotsRoot { get; init; } = "";

    public IReadOnlyDictionary<int, Survivor> Survivors { get; init; } = new Dictionary<int, Survivor>();

    /// <summary>Every <see cref="SettingKeys"/> entry as the cfg file holds it, by id.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    /// <summary>The run being played, when the manager was opened from the pause menu; empty on the title screen.</summary>
    public string InRunStem { get; init; } = "";

    /// <summary>Each run's save files, by stem.</summary>
    public IReadOnlyDictionary<string, SaveDisk> Disk { get; init; } = new Dictionary<string, SaveDisk>();

    public DateTime NowUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>One of the game's own snapshots of a run: a day snapshot (rebirth can go back to it) or the round snapshot.</summary>
public sealed record GameSnap(string File, bool IsRound, Progress Progress, long Bytes, DateTime WrittenUtc, SaveStats? Stats);

/// <summary>What the detail view of one run needs beyond the list: the save's stats now, the game's snapshots, ending photos and item names.</summary>
public sealed record RunDetail(
    string Stem,
    SaveStats? Now,
    IReadOnlyList<BackupMeta> Backups,
    IReadOnlyList<GameSnap> Snaps,
    IReadOnlyList<string> Photos,
    IReadOnlyDictionary<int, Phrase> ItemNames);

/// <summary>
/// The save manager's view as JSON, written straight from the screen. Each run lists its backups newest first and a
/// warning when its save is behind its last backup (<see cref="CloudCheck"/>). Backups whose run is no longer in the
/// game's list are the "deleted saves", which can be brought back.
/// </summary>
public static class PageModel
{
    private const string AtFormat = "yyyy-MM-dd HH:mm";

    public static string Json(Screen screen)
    {
        var byStem = new Dictionary<string, List<BackupMeta>>(StringComparer.OrdinalIgnoreCase);

        foreach (var meta in screen.Backups)
        {
            if (!byStem.TryGetValue(meta.Stem, out var list)) byStem[meta.Stem] = list = new List<BackupMeta>();

            list.Add(meta);
        }

        foreach (var list in byStem.Values) list.Sort((a, b) => b.CreatedUtc.CompareTo(a.CreatedUtc));

        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteBoolean("zh", screen.IsChinese);
            json.WriteString("key", screen.BackupKey);
            json.WriteString("folder", screen.Folder);
            json.WriteString("version", screen.GameVersion);
            json.WriteNumber("bytes", screen.Backups.Sum(meta => meta.Bytes));
            json.WriteNumber("limit", screen.LimitBytes);
            json.WriteNumber("saveBytes", screen.Disk.Values.Sum(disk => disk.Bytes));
            json.WriteString("shots", screen.ShotsRoot);
            json.WriteString("inRun", screen.InRunStem);
            WriteSurvivors(json, screen.Survivors);
            WriteSettings(json, screen.Settings);
            json.WriteStartArray("runs");

            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var run in screen.Runs)
            {
                var stem = RunFiles.Stem(run.FileName);
                var backups = byStem.TryGetValue(stem, out var found) ? found : new List<BackupMeta>();
                DateTime? settled = screen.Settled.TryGetValue(stem, out var at) ? at : null;
                var note = screen.Notes.TryGetValue(stem, out var kept) ? kept : RunNote.Empty;

                listed.Add(stem);
                json.WriteStartObject();
                json.WriteString("file", run.FileName);
                json.WriteString("stem", stem);
                json.WriteString("name", run.Name);
                json.WriteNumber("day", run.Progress.Day + 1);
                json.WriteNumber("turn", run.Progress.Turn);
                json.WriteString("clock", run.Progress.Clock);
                json.WriteString("role", run.Role);
                json.WriteNumber("survivor", run.SurvivorId);
                json.WriteString("time", run.Time);
                json.WriteString("turnText", run.Turn);
                json.WriteString("status", run.Status);
                json.WriteString("diff", run.Difficulty);
                json.WriteString("phase", run.Phase);
                json.WriteBoolean("finished", run.IsFinished);
                json.WriteBoolean("endless", run.IsEndless);
                json.WriteBoolean("pinned", note.IsPinned);
                json.WriteString("note", note.Note);
                json.WriteNumber("played", note.PlayedSeconds);
                json.WriteBoolean("inRun", string.Equals(stem, screen.InRunStem, StringComparison.OrdinalIgnoreCase));

                if (screen.Disk.TryGetValue(stem, out var disk))
                {
                    json.WriteNumber("saveBytes", disk.Bytes);
                    json.WriteString("lastPlayed", Local(disk.WrittenUtc));
                    json.WriteNumber("lastPlayedAgo", Math.Max(0, (long)(screen.NowUtc - disk.WrittenUtc).TotalSeconds));
                }

                if (CloudCheck.Behind(run.Progress, backups, settled) is { } ahead)
                {
                    json.WriteStartObject("behind");
                    json.WriteString("id", ahead.Id);
                    json.WriteNumber("day", ahead.Run.Progress.Day + 1);
                    json.WriteNumber("turn", ahead.Run.Progress.Turn);
                    json.WriteString("clock", ahead.Run.Progress.Clock);
                    json.WriteEndObject();
                }

                WriteBackups(json, backups, screen.GameVersion);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("deleted");

            foreach (var pair in byStem.Where(pair => !listed.Contains(pair.Key)).OrderByDescending(pair => pair.Value[0].CreatedUtc))
            {
                var newest = pair.Value[0];
                var note = screen.Notes.TryGetValue(pair.Key, out var kept) ? kept : RunNote.Empty;

                json.WriteStartObject();
                json.WriteString("stem", pair.Key);
                json.WriteString("name", newest.Run.Name);
                json.WriteNumber("day", newest.Run.Progress.Day + 1);
                json.WriteNumber("turn", newest.Run.Progress.Turn);
                json.WriteNumber("survivor", newest.Run.SurvivorId);
                json.WriteNumber("daysLeft", DaysLeft(note.DeletedAt ?? newest.CreatedUtc, screen.BinDays, screen.NowUtc));
                WriteBackups(json, pair.Value, screen.GameVersion);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteNumber("binDays", screen.BinDays);
            json.WriteNumber("binBytes", screen.Bin.Sum(entry => entry.Meta.Bytes));
            json.WriteStartArray("bin");

            foreach (var entry in screen.Bin)
            {
                json.WriteStartObject();
                json.WriteString("stem", entry.Meta.Stem);
                json.WriteString("id", entry.Meta.Id);
                json.WriteString("kind", BackupKinds.Id(entry.Meta.Kind));
                json.WriteString("name", entry.Meta.Run.Name);
                json.WriteNumber("day", entry.Meta.Run.Progress.Day + 1);
                json.WriteNumber("turn", entry.Meta.Run.Progress.Turn);
                json.WriteString("clock", entry.Meta.Run.Progress.Clock);
                json.WriteString("at", Local(entry.Meta.CreatedUtc));
                json.WriteString("binned", Local(entry.BinnedUtc));
                json.WriteNumber("bytes", entry.Meta.Bytes);
                json.WriteString("why", entry.Why == BinReason.Thinned ? "thinned" : "deleted");
                json.WriteBoolean("shot", entry.HasShot);
                json.WriteNumber("daysLeft", DaysLeft(entry.BinnedUtc, screen.BinDays, screen.NowUtc));
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>The detail view's extra data for one run. Items are listed per save by id, with names for every id that appears.</summary>
    public static string Detail(RunDetail detail)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("stem", detail.Stem);
            if (detail.Now != null) WriteStats(json, "now", detail.Now, withItems: true);
            json.WriteStartObject("items");

            foreach (var meta in detail.Backups.Where(meta => meta.Stats != null)) WriteItems(json, meta.Id, meta.Stats!);

            json.WriteEndObject();
            json.WriteStartObject("names");

            foreach (var pair in detail.ItemNames.OrderBy(pair => pair.Key))
            {
                json.WriteStartArray(pair.Key.ToString(CultureInfo.InvariantCulture));
                json.WriteStringValue(pair.Value.En);
                json.WriteStringValue(pair.Value.Zh);
                json.WriteEndArray();
            }

            json.WriteEndObject();
            json.WriteStartArray("snaps");

            foreach (var snap in detail.Snaps)
            {
                json.WriteStartObject();
                json.WriteString("file", snap.File);
                json.WriteBoolean("round", snap.IsRound);
                json.WriteNumber("day", snap.Progress.Day + 1);
                json.WriteNumber("turn", snap.Progress.Turn);
                json.WriteString("clock", snap.Progress.Clock);
                json.WriteNumber("bytes", snap.Bytes);
                json.WriteString("at", Local(snap.WrittenUtc));
                if (snap.Stats != null) WriteStats(json, "stats", snap.Stats, withItems: false);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("photos");

            foreach (var photo in detail.Photos) json.WriteStringValue(photo);

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>What the Import dialog lists: each file in the Import folder, with the run inside a .slsave when it could be read.</summary>
    public static string Imports(IReadOnlyList<ImportCandidate> candidates, string importFolder)
    {
        using var buffer = new MemoryStream();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("folder", importFolder);
            json.WriteStartArray("files");

            foreach (var candidate in candidates)
            {
                json.WriteStartObject();
                json.WriteString("file", candidate.File);
                json.WriteBoolean("loose", candidate.IsLoose);
                json.WriteNumber("bytes", candidate.Bytes);
                json.WriteString("at", Local(candidate.WrittenUtc));
                json.WriteNumber("points", candidate.Points);

                if (candidate.Newest is { } newest)
                {
                    json.WriteString("name", newest.Run.Name);
                    json.WriteNumber("day", newest.Run.Progress.Day + 1);
                    json.WriteNumber("turn", newest.Run.Progress.Turn);
                    json.WriteNumber("survivor", newest.Run.SurvivorId);
                    json.WriteString("version", newest.GameVersion);
                }

                // A .slsave that could not be read has no backups in it; the page shows it as one that will not import.
                json.WriteBoolean("readable", candidate.IsLoose || candidate.Newest != null);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>A backup made on another game version than the one running; restoring it may not load cleanly. Unknown versions never count.</summary>
    public static bool IsOldVersion(BackupMeta meta, string gameVersion) =>
        meta.GameVersion.Length > 0 && gameVersion.Length > 0 && !string.Equals(meta.GameVersion, gameVersion, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whole days left before something put away at <paramref name="since"/> goes for good, at least 0; -1 when the bin keeps it until emptied.</summary>
    public static int DaysLeft(DateTime since, int binDays, DateTime nowUtc)
    {
        if (binDays <= 0) return -1;

        var left = since.AddDays(binDays) - nowUtc;

        return Math.Max(0, (int)Math.Ceiling(left.TotalDays));
    }

    private static string Local(DateTime utc) => utc.ToLocalTime().ToString(AtFormat, CultureInfo.InvariantCulture);

    private static void WriteBackups(Utf8JsonWriter json, List<BackupMeta> backups, string gameVersion)
    {
        json.WriteStartArray("backups");

        foreach (var meta in backups)
        {
            json.WriteStartObject();
            json.WriteString("id", meta.Id);
            json.WriteString("kind", BackupKinds.Id(meta.Kind));
            json.WriteString("at", Local(meta.CreatedUtc));
            json.WriteNumber("day", meta.Run.Progress.Day + 1);
            json.WriteNumber("turn", meta.Run.Progress.Turn);
            json.WriteString("clock", meta.Run.Progress.Clock);
            json.WriteBoolean("progress", BackupKinds.ShowsProgress(meta.Kind));
            json.WriteBoolean("auto", BackupKinds.IsAutomatic(meta.Kind));
            json.WriteNumber("bytes", meta.Bytes);
            json.WriteBoolean("pinned", meta.IsPinned);
            json.WriteBoolean("shot", meta.HasShot);
            json.WriteString("note", meta.Note);
            json.WriteString("dayText", meta.DayText);
            json.WriteString("version", meta.GameVersion);
            json.WriteBoolean("oldVersion", IsOldVersion(meta, gameVersion));
            if (meta.Stats != null) WriteStats(json, "stats", meta.Stats, withItems: false);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    /// <summary>The stats the page shows: meters as [value, max], money, points, the item count and the journal line.</summary>
    private static void WriteStats(Utf8JsonWriter json, string name, SaveStats stats, bool withItems)
    {
        json.WriteStartObject(name);
        WriteMeter(json, "health", stats.Health);
        WriteMeter(json, "fullness", stats.Fullness);
        WriteMeter(json, "mood", stats.Mood);
        WriteMeter(json, "stamina", stats.Stamina);
        json.WriteNumber("money", stats.Money);
        json.WriteNumber("points", stats.Points);
        json.WriteNumber("items", stats.ItemTotal);
        json.WriteString("journal", stats.Journal);
        json.WriteEndObject();

        if (withItems) WriteItems(json, name + "Items", stats);
    }

    private static void WriteItems(Utf8JsonWriter json, string name, SaveStats stats)
    {
        json.WriteStartObject(name);

        foreach (var pair in stats.Items.OrderBy(pair => pair.Key)) json.WriteNumber(pair.Key.ToString(CultureInfo.InvariantCulture), pair.Value);

        json.WriteEndObject();
    }

    private static void WriteMeter(Utf8JsonWriter json, string name, Meter meter)
    {
        json.WriteStartArray(name);
        json.WriteNumberValue(meter.Value);
        json.WriteNumberValue(meter.Max);
        json.WriteEndArray();
    }

    private static void WriteSurvivors(Utf8JsonWriter json, IReadOnlyDictionary<int, Survivor> survivors)
    {
        json.WriteStartObject("survivors");

        foreach (var pair in survivors.OrderBy(pair => pair.Key))
        {
            json.WriteStartObject(pair.Key.ToString(CultureInfo.InvariantCulture));
            json.WriteString("en", pair.Value.NameEn);
            json.WriteString("zh", pair.Value.NameZh);
            json.WriteString("pic", pair.Value.Portrait);
            json.WriteString("bg", pair.Value.Background);
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteSettings(Utf8JsonWriter json, IReadOnlyDictionary<string, string> settings)
    {
        json.WriteStartObject("settings");

        foreach (var key in SettingKeys.All)
            if (settings.TryGetValue(key.Id, out var value)) json.WriteString(key.Id, value);

        json.WriteEndObject();
    }
}
