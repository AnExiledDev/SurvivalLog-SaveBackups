using System;
using System.Globalization;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>
/// What the mod keeps about a run as a whole, in its folder's run.json: when its save was last put right (see
/// <see cref="BackupStore.SettledAt"/>), whether the player starred it, their note, how long it has been played
/// since the mod started counting, and when the mod saw it leave the game's list.
/// </summary>
public sealed record RunNote(DateTime? Settled, bool IsPinned, string Note, long PlayedSeconds, DateTime? DeletedAt)
{
    public static readonly RunNote Empty = new(null, false, "", 0, null);

    public string ToJson()
    {
        using var buffer = new System.IO.MemoryStream();

        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            if (Settled is { } settled) json.WriteString("settled", settled.ToString("o", CultureInfo.InvariantCulture));
            if (IsPinned) json.WriteBoolean("pinned", true);
            if (Note.Length > 0) json.WriteString("note", Note);
            if (PlayedSeconds > 0) json.WriteNumber("played", PlayedSeconds);
            if (DeletedAt is { } deleted) json.WriteString("deleted", deleted.ToString("o", CultureInfo.InvariantCulture));
            json.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>A run.json that cannot be read counts as empty, so a damaged one never stops a backup.</summary>
    public static RunNote Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return Empty;

            return new RunNote(
                Time(root, "settled"),
                root.TryGetProperty("pinned", out var pinned) && pinned.ValueKind == JsonValueKind.True,
                Notes.Clean(root.TryGetProperty("note", out var note) && note.ValueKind == JsonValueKind.String ? note.GetString() : ""),
                root.TryGetProperty("played", out var played) && played.ValueKind == JsonValueKind.Number && played.TryGetInt64(out var seconds) ? Math.Max(0, seconds) : 0,
                Time(root, "deleted"));
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    private static DateTime? Time(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
            ? at.ToUniversalTime()
            : null;
}
