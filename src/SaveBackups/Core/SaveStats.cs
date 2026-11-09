using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>A number the survivor has and the most it can be (0 when the game keeps no most).</summary>
public readonly record struct Meter(int Value, int Max)
{
    /// <summary>The scale the game keeps a meter's value at in a save when it does not say (GameKey.Attr_ScalingRatio).</summary>
    public const float DefaultScale = 1000f;

    /// <summary>
    /// A meter as the game shows it from the numbers a save keeps: the value is stored times <paramref name="scale"/>
    /// (60 Life is kept as 60000) and the most as it is. The value is rounded, as the game's bars do, and held to the
    /// most, as the game holds it when it reads the value.
    /// </summary>
    public static Meter FromSave(int stored, int max, float scale)
    {
        if (!(scale > 0)) scale = DefaultScale;

        var value = (int)Math.Round(stored / (double)scale, MidpointRounding.AwayFromZero);

        return new Meter(max > 0 ? Math.Clamp(value, 0, max) : Math.Max(0, value), max);
    }
}

/// <summary>
/// How the survivor and home stood in one save: the meters, the Planning Points loop rewards are bought with, the
/// survival points the end-of-day cards are bought with, and every item counted by its config id (carried and stored
/// together). Read from a save file by the game's reader, kept in backup.json so a backup is read once.
/// </summary>
public sealed record SaveStats(
    Meter Life,
    Meter Satiety,
    Meter Mood,
    Meter Stamina,
    int PlanningPoints,
    int SurvivalPoints,
    IReadOnlyDictionary<int, int> Items)
{
    // Kept since stats read the full most of each meter (LiveMax); stats kept without it are read again.
    private const string LifeKey = "lifeFullMax";
    private const string PlanningKey = "planning";

    public int ItemTotal => Items.Values.Sum();

    public void Write(Utf8JsonWriter json, string name)
    {
        json.WriteStartObject(name);
        WriteMeter(json, LifeKey, Life);
        WriteMeter(json, "satiety", Satiety);
        WriteMeter(json, "mood", Mood);
        WriteMeter(json, "stamina", Stamina);
        json.WriteNumber(PlanningKey, PlanningPoints);
        json.WriteNumber("survival", SurvivalPoints);
        json.WriteNumber("itemTotal", ItemTotal);
        json.WriteStartObject("items");

        foreach (var pair in Items.OrderBy(pair => pair.Key)) json.WriteNumber(pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), pair.Value);

        json.WriteEndObject();
        json.WriteEndObject();
    }

    /// <summary>
    /// The stats from a backup.json "stats" object, or null when it is missing, not one, or written before the full most of
    /// each meter was kept (null makes the save manager read the backup's save again and keep the full stats).
    /// </summary>
    public static SaveStats? Read(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var stats) || stats.ValueKind != JsonValueKind.Object || !stats.TryGetProperty(LifeKey, out _)) return null;

        var items = new Dictionary<int, int>();

        if (stats.TryGetProperty("items", out var list) && list.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in list.EnumerateObject())
                if (int.TryParse(item.Name, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var id) && item.Value.TryGetInt32(out var count))
                    items[id] = count;
        }

        return new SaveStats(
            ReadMeter(stats, LifeKey),
            ReadMeter(stats, "satiety"),
            ReadMeter(stats, "mood"),
            ReadMeter(stats, "stamina"),
            Number(stats, PlanningKey),
            Number(stats, "survival"),
            items);
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
