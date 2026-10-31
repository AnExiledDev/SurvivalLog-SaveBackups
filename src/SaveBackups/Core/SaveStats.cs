using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>A number the survivor has and the most it can be (0 when the game keeps no most).</summary>
public readonly record struct Meter(int Value, int Max);

/// <summary>
/// How the survivor and home stood in one save: the meters, money, survival points and every item counted by its
/// config id (carried and stored together). Read from a save file by the game's own reader, kept in backup.json so a
/// backup is read once.
/// </summary>
public sealed record SaveStats(
    Meter Health,
    Meter Fullness,
    Meter Mood,
    Meter Stamina,
    int Money,
    int Points,
    IReadOnlyDictionary<int, int> Items,
    string Journal)
{
    public int ItemTotal => Items.Values.Sum();

    public void Write(Utf8JsonWriter json, string name)
    {
        json.WriteStartObject(name);
        WriteMeter(json, "health", Health);
        WriteMeter(json, "fullness", Fullness);
        WriteMeter(json, "mood", Mood);
        WriteMeter(json, "stamina", Stamina);
        json.WriteNumber("money", Money);
        json.WriteNumber("points", Points);
        json.WriteNumber("itemTotal", ItemTotal);
        json.WriteStartObject("items");

        foreach (var pair in Items.OrderBy(pair => pair.Key)) json.WriteNumber(pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), pair.Value);

        json.WriteEndObject();
        if (Journal.Length > 0) json.WriteString("journal", Journal);
        json.WriteEndObject();
    }

    /// <summary>The stats from a backup.json "stats" object, or null when it is missing or not one.</summary>
    public static SaveStats? Read(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var stats) || stats.ValueKind != JsonValueKind.Object) return null;

        var items = new Dictionary<int, int>();

        if (stats.TryGetProperty("items", out var list) && list.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in list.EnumerateObject())
                if (int.TryParse(item.Name, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var id) && item.Value.TryGetInt32(out var count))
                    items[id] = count;
        }

        return new SaveStats(
            ReadMeter(stats, "health"),
            ReadMeter(stats, "fullness"),
            ReadMeter(stats, "mood"),
            ReadMeter(stats, "stamina"),
            Number(stats, "money"),
            Number(stats, "points"),
            items,
            stats.TryGetProperty("journal", out var journal) && journal.ValueKind == JsonValueKind.String ? journal.GetString() ?? "" : "");
    }

    /// <summary>What changed in the home between two saves: item ids with how many more (plus) or fewer (minus), biggest change first.</summary>
    public static IReadOnlyList<(int Id, int Change)> ItemChanges(SaveStats then, SaveStats now)
    {
        var changes = new List<(int Id, int Change)>();

        foreach (var id in then.Items.Keys.Union(now.Items.Keys))
        {
            var change = (now.Items.TryGetValue(id, out var after) ? after : 0) - (then.Items.TryGetValue(id, out var before) ? before : 0);

            if (change != 0) changes.Add((id, change));
        }

        return changes.OrderByDescending(pair => Math.Abs(pair.Change)).ThenBy(pair => pair.Id).ToList();
    }

    private static void WriteMeter(Utf8JsonWriter json, string name, Meter meter)
    {
        json.WriteStartArray(name);
        json.WriteNumberValue(meter.Value);
        json.WriteNumberValue(meter.Max);
        json.WriteEndArray();
    }

    private static Meter ReadMeter(JsonElement stats, string name)
    {
        if (!stats.TryGetProperty(name, out var pair) || pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2) return default;

        return new Meter(pair[0].TryGetInt32(out var value) ? value : 0, pair[1].TryGetInt32(out var max) ? max : 0);
    }

    private static int Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;
}
