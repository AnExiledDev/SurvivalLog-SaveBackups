using System.IO;

namespace SaveBackups.Core;

/// <summary>Run and backup names come back from the page as folder and file names; anything that could point outside its folder is refused.</summary>
public static class Names
{
    public static bool IsSafe(string? name) =>
        !string.IsNullOrEmpty(name)
        && name != "."
        && name != ".."
        && name.IndexOf('/') < 0
        && name.IndexOf('\\') < 0
        && name.IndexOf(':') < 0
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
}
