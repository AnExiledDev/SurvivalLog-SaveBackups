using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>
/// The files on disk that make up one run. The game keeps a run as Save_X.bytes plus files named after it: the round
/// file (Save_X.round.bytes), the rebirth day snapshots (Save_X.snap0.bytes to snap5) and the ending snapshots
/// (Save_X.bytes.ending.N). A backup copies all of them, so a restored run's snapshots match its main file. The
/// game's own .bak copy of the main file and its half-written temp files are left out.
/// </summary>
public static class RunFiles
{
    private const string MainExtension = ".bytes";

    /// <summary>The run's name without the .bytes the save list stores it with: "Save_X.bytes" and "Save_X" both give "Save_X".</summary>
    public static string Stem(string fileName)
    {
        var name = Path.GetFileName(fileName ?? "");

        return name.EndsWith(MainExtension, StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - MainExtension.Length) : name;
    }

    /// <summary>The main file's name, as the game opens it.</summary>
    public static string MainFile(string stem) => stem + MainExtension;

    /// <summary>Whether a file in the save folder belongs to the run.</summary>
    public static bool Belongs(string stem, string fileName)
    {
        if (string.IsNullOrEmpty(stem) || !fileName.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase)) return false;

        return !fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
               && !fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
               && !fileName.Contains(".tmp", StringComparison.OrdinalIgnoreCase)
               && !fileName.EndsWith(BackupStore.TempSuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The run's files in a folder, names only, sorted.</summary>
    public static IReadOnlyList<string> In(string folder, string stem)
    {
        if (!Directory.Exists(folder)) return Array.Empty<string>();

        return Directory.EnumerateFiles(folder)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => Belongs(stem, name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
