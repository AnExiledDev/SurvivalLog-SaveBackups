using System;
using System.Text;

namespace SaveBackups.Core;

/// <summary>
/// A backup's note as the player typed it. The page sends it percent-encoded (encodeURIComponent), so the "|" that
/// splits a page message cannot appear in it. Line breaks and other control characters become spaces, and it is cut
/// to <see cref="MaxLength"/> characters.
/// </summary>
public static class Notes
{
    public const int MaxLength = 200;

    /// <summary>The note from its encoded form, or null when the text is not percent-encoding at all.</summary>
    public static string? Decode(string encoded)
    {
        try
        {
            return Clean(Uri.UnescapeDataString(encoded));
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    public static string Clean(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var clean = new StringBuilder(Math.Min(text.Length, MaxLength));

        foreach (var c in text) clean.Append(char.IsControl(c) ? ' ' : c);

        var note = clean.ToString().Trim();

        if (note.Length <= MaxLength) return note;

        // A cut never leaves half of a surrogate pair at the end.
        var cut = char.IsHighSurrogate(note[MaxLength - 1]) ? MaxLength - 1 : MaxLength;

        return note.Substring(0, cut).TrimEnd();
    }
}
