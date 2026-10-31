using System;
using System.Globalization;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>What the game knew about a run when a backup was made, copied from its save list entry.</summary>
public sealed record RunFacts(string FileName, string Name, Progress Progress, int SurvivorId, int PresetId, bool IsFinished);

/// <summary>
/// A backup's backup.json: when and why it was made and what the run looked like. <see cref="Id"/> is the folder name
/// and is not written into the file.
/// </summary>
public sealed record BackupMeta(
    string Id,
    string Stem,
    BackupKind Kind,
    DateTime CreatedUtc,
    RunFacts Run,
    long Bytes,
    string GameVersion,
    bool IsPinned)
{
    public const string FileName = "backup.json";
    private const int Format = 1;

    /// <summary>The player's own words about the backup, empty when there are none.</summary>
    public string Note { get; init; } = "";

    /// <summary>The game's own line for the day the run was on, on morning backups; empty on the others.</summary>
    public string DayText { get; init; } = "";

    /// <summary>How the survivor and home stood in the backup, once read; null until then.</summary>
    public SaveStats? Stats { get; init; }

    /// <summary>Whether a picture was taken with the backup. Read from the folder, not written into backup.json.</summary>
    public bool HasShot { get; init; }

    public string ToJson()
    {
        using var buffer = new System.IO.MemoryStream();

        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("format", Format);
            json.WriteString("kind", BackupKinds.Id(Kind));
            json.WriteString("created", CreatedUtc.ToString("o", CultureInfo.InvariantCulture));
            json.WriteString("fileName", Run.FileName);
            json.WriteString("name", Run.Name);
            json.WriteNumber("turn", Run.Progress.Turn);
            json.WriteNumber("day", Run.Progress.Day);
            json.WriteNumber("gameTime", Run.Progress.GameTime);
            json.WriteNumber("survivor", Run.SurvivorId);
            json.WriteNumber("preset", Run.PresetId);
            json.WriteBoolean("finished", Run.IsFinished);
            json.WriteNumber("bytes", Bytes);
            json.WriteString("gameVersion", GameVersion);
            json.WriteBoolean("pinned", IsPinned);
            if (Note.Length > 0) json.WriteString("note", Note);
            if (DayText.Length > 0) json.WriteString("dayText", DayText);
            Stats?.Write(json, "stats");
            json.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Reads a backup.json. A file that is not one (hand-edited, cut short) gives null, so one bad backup never hides the others.</summary>
    public static BackupMeta? Parse(string id, string stem, string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!BackupKinds.TryParse(Text(root, "kind"), out var kind)) return null;
            if (!DateTime.TryParse(Text(root, "created"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var created)) return null;

            var fileName = Text(root, "fileName");

            if (string.IsNullOrEmpty(fileName)) fileName = RunFiles.MainFile(stem);

            var run = new RunFacts(
                fileName,
                Text(root, "name"),
                new Progress(Number(root, "turn"), Number(root, "day"), Number(root, "gameTime")),
                Number(root, "survivor"),
                Number(root, "preset"),
                Flag(root, "finished"));

            return new BackupMeta(id, stem, kind, created.ToUniversalTime(), run, Long(root, "bytes"), Text(root, "gameVersion"), Flag(root, "pinned"))
            {
                Note = Notes.Clean(Text(root, "note")),
                DayText = Text(root, "dayText"),
                Stats = SaveStats.Read(root, "stats"),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static int Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;

    private static long Long(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : 0;

    private static bool Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
