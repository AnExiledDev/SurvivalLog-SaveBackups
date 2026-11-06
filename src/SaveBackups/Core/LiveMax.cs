using System.Globalization;

namespace SaveBackups.Core;

/// <summary>
/// The most each meter can reach. A save keeps only the base most (100); what buffs, talents and the run add on top
/// lives in the running game alone. While a run is open Save Backups keeps the full most in the run's counter map under
/// <see cref="Key"/>, so every save and backup made from then on carries it. A save made without it has no known most.
/// </summary>
public static class LiveMax
{
    public const string Prefix = "SaveBackups.Max.";

    /// <summary>The counter key for the most of a meter, by the game's AttrName id of that most (105 for Life's).</summary>
    public static string Key(int maxAttrId) => Prefix + maxAttrId.ToString(CultureInfo.InvariantCulture);

    /// <summary>A kept most as the save manager uses it: 0 (unknown) unless one above 0 was kept.</summary>
    public static int Known(bool isKept, int kept) => isKept && kept > 0 ? kept : 0;
}
