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
    bool IsEndless);

/// <summary>Everything the save manager draws from: the game's runs, the backups on disk and each run's settled time.</summary>
public sealed record Screen(
    IReadOnlyList<RunView> Runs,
    IReadOnlyList<BackupMeta> Backups,
    IReadOnlyDictionary<string, DateTime> Settled,
    string Folder,
    string BackupKey,
    bool IsChinese);

/// <summary>
/// The save manager's view as JSON, written straight from the screen. Each run lists its backups newest first and a
/// warning when its save is behind its last backup (<see cref="CloudCheck"/>). Backups whose run is no longer in the
/// game's list are the "deleted saves", which can be brought back.
/// </summary>
public static class PageModel
{
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
            json.WriteNumber("bytes", screen.Backups.Sum(meta => meta.Bytes));
            json.WriteStartArray("runs");

            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var run in screen.Runs)
            {
                var stem = RunFiles.Stem(run.FileName);
                var backups = byStem.TryGetValue(stem, out var found) ? found : new List<BackupMeta>();
                DateTime? settled = screen.Settled.TryGetValue(stem, out var at) ? at : null;

                listed.Add(stem);
                json.WriteStartObject();
                json.WriteString("file", run.FileName);
                json.WriteString("stem", stem);
                json.WriteString("name", run.Name);
                json.WriteNumber("day", run.Progress.Day + 1);
                json.WriteNumber("turn", run.Progress.Turn);
                json.WriteString("role", run.Role);
                json.WriteString("time", run.Time);
                json.WriteString("turnText", run.Turn);
                json.WriteString("status", run.Status);
                json.WriteString("diff", run.Difficulty);
                json.WriteString("phase", run.Phase);
                json.WriteBoolean("finished", run.IsFinished);
                json.WriteBoolean("endless", run.IsEndless);

                if (CloudCheck.Behind(run.Progress, backups, settled) is { } ahead)
                {
                    json.WriteStartObject("behind");
                    json.WriteString("id", ahead.Id);
                    json.WriteNumber("day", ahead.Run.Progress.Day + 1);
                    json.WriteNumber("turn", ahead.Run.Progress.Turn);
                    json.WriteEndObject();
                }

                WriteBackups(json, backups);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("deleted");

            foreach (var pair in byStem.Where(pair => !listed.Contains(pair.Key)).OrderByDescending(pair => pair.Value[0].CreatedUtc))
            {
                var newest = pair.Value[0];

                json.WriteStartObject();
                json.WriteString("stem", pair.Key);
                json.WriteString("name", newest.Run.Name);
                json.WriteNumber("day", newest.Run.Progress.Day + 1);
                json.WriteNumber("turn", newest.Run.Progress.Turn);
                WriteBackups(json, pair.Value);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteBackups(Utf8JsonWriter json, List<BackupMeta> backups)
    {
        json.WriteStartArray("backups");

        foreach (var meta in backups)
        {
            json.WriteStartObject();
            json.WriteString("id", meta.Id);
            json.WriteString("kind", BackupKinds.Id(meta.Kind));
            json.WriteString("at", meta.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            json.WriteNumber("day", meta.Run.Progress.Day + 1);
            json.WriteNumber("turn", meta.Run.Progress.Turn);
            json.WriteBoolean("progress", BackupKinds.ShowsProgress(meta.Kind));
            json.WriteNumber("bytes", meta.Bytes);
            json.WriteBoolean("pinned", meta.IsPinned);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }
}
