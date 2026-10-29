using System;
using System.Globalization;

namespace SaveBackups.Core;

/// <summary>
/// The file name a backup copied as a new save gets. It is named the way the game names a new save
/// (Save_yyyy_MM_dd_HH_mm_ss), with "_2", "_3" and on added while the name is taken, as the game does when it forks a
/// run, so the copy sorts and looks like any other save.
/// </summary>
public static class NewSave
{
    public static string? FreeStem(DateTime localNow, Func<string, bool> isTaken)
    {
        var stem = "Save_" + localNow.ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);

        if (!isTaken(stem)) return stem;

        for (var n = 2; n < 100; n++)
            if (!isTaken(stem + "_" + n)) return stem + "_" + n;

        return null;
    }

    /// <summary>A run file's name with the run's stem swapped: Save_A.snap2.bytes becomes Save_B.snap2.bytes.</summary>
    public static string Rename(string fileName, string fromStem, string toStem) =>
        fileName.StartsWith(fromStem + ".", StringComparison.OrdinalIgnoreCase) ? toStem + fileName.Substring(fromStem.Length) : fileName;

    /// <summary>
    /// The ending snapshots (Save_X.bytes.ending.N) are not copied: the game finds them through the run's entry in its
    /// save list, which the copy does not inherit.
    /// </summary>
    public static bool IsCopied(string stem, string fileName) =>
        RunFiles.Belongs(stem, fileName) && fileName.IndexOf(".bytes.ending.", StringComparison.OrdinalIgnoreCase) < 0;
}
