using System;

namespace SaveBackups.Core;

/// <summary>
/// What the save manager page asks for. It sends "slmod|savebackups|" followed by a verb and its arguments, split on
/// "|". Anything else parses to Unknown so it can be logged rather than acted on. Run and backup names are checked
/// here, so no command can name a path.
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|savebackups|";

    private PageCommand()
    {
    }

    /// <summary>The page script just installed itself.</summary>
    public sealed record Ready : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    /// <summary>The manager's close button.</summary>
    public sealed record Close : PageCommand;

    /// <summary>Load this save, as the game's own Load button would.</summary>
    public sealed record Load(string FileName) : PageCommand;

    /// <summary>Back up this run now.</summary>
    public sealed record BackUp(string Stem) : PageCommand;

    /// <summary>Put this backup in place of the run's save.</summary>
    public sealed record Restore(string Stem, string Id) : PageCommand;

    /// <summary>Delete the save (its backups stay, so it can be brought back).</summary>
    public sealed record DeleteSave(string Stem) : PageCommand;

    /// <summary>Put a deleted save back from this backup, and back in the game's list.</summary>
    public sealed record BringBack(string Stem, string Id) : PageCommand;

    public sealed record Pin(string Stem, string Id, bool IsPinned) : PageCommand;

    /// <summary>Delete one backup.</summary>
    public sealed record Remove(string Stem, string Id) : PageCommand;

    /// <summary>Open the backup folder in the file explorer, or one run's folder in it.</summary>
    public sealed record OpenFolder(string Stem = "") : PageCommand;

    /// <summary>Close the manager and open the game's own save list.</summary>
    public sealed record GameList : PageCommand;

    /// <summary>The player read the "save went backwards" warning for this run and wants it gone.</summary>
    public sealed record Dismiss(string Stem) : PageCommand;

    /// <summary>Copy this backup out as a new save, next to the others in the game's list.</summary>
    public sealed record CopyAsNew(string Stem, string Id) : PageCommand;

    /// <summary>Write this backup as a .slsave file in the Exports folder.</summary>
    public sealed record Export(string Stem, string Id) : PageCommand;

    /// <summary>Write the run's save as it is now as a .slsave, with every backup when <see cref="IsAll"/>, and the pictures and notes when <see cref="WithExtras"/>.</summary>
    public sealed record ExportRun(string Stem, bool IsAll, bool WithExtras) : PageCommand;

    /// <summary>List what is in the Import folder.</summary>
    public sealed record ImportList : PageCommand;

    /// <summary>Import one file from the Import folder: a .slsave, or a loose save file added as a new save.</summary>
    public sealed record ImportFile(string File) : PageCommand;

    /// <summary>Give the run a new name in the game's list.</summary>
    public sealed record Rename(string Stem, string Name) : PageCommand;

    /// <summary>Star the run, so it sorts first.</summary>
    public sealed record RunPin(string Stem, bool IsPinned) : PageCommand;

    /// <summary>The player's note on the whole run.</summary>
    public sealed record SaveNote(string Stem, string Text) : PageCommand;

    /// <summary>Copy the run's save as it is now as a new save.</summary>
    public sealed record Duplicate(string Stem) : PageCommand;

    /// <summary>The run's details the list does not carry: its stats now, the game's own snapshots, ending photos and item names.</summary>
    public sealed record Detail(string Stem) : PageCommand;

    /// <summary>Copy one of the game's own snapshots of the run out as a new save.</summary>
    public sealed record SnapshotAsNew(string Stem, string File) : PageCommand;

    public sealed record BinPutBack(string Stem, string Id) : PageCommand;

    public sealed record BinDelete(string Stem, string Id) : PageCommand;

    /// <summary>Delete a deleted save's backups for good, pinned ones too.</summary>
    public sealed record BinDeleteRun(string Stem) : PageCommand;

    public sealed record BinEmpty : PageCommand;

    /// <summary>Change one setting (<see cref="SettingKeys"/>).</summary>
    public sealed record Set(string Id, string Value) : PageCommand;

    /// <summary>Start a new game, as the title screen's New Game button does.</summary>
    public sealed record NewGame : PageCommand;

    /// <summary>Save and back up the run being played, as the backup key does.</summary>
    public sealed record BackUpNow : PageCommand;

    /// <summary>Open the manager on the run being played (the pause menu button).</summary>
    public sealed record OpenInRun : PageCommand;

    /// <summary>Add every .slsave file in the Import folder to the backups.</summary>
    public sealed record Import : PageCommand;

    /// <summary>Set a backup's note; an empty note removes it.</summary>
    public sealed record Note(string Stem, string Id, string Text) : PageCommand;

    /// <summary>A text box in the page took (true) or lost (false) the keyboard.</summary>
    public sealed record Typing(bool IsTyping) : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    /// <summary>The parts that are not names: flags, and texts percent-encoded so they have no "|".</summary>
    private static bool IsFree(string verb, int part) =>
        (verb, part) is ("pin", 3) or ("note", 3) or ("typing", 1) or ("rename", 2) or ("runpin", 2) or ("runnote", 2) or ("set", 2) or ("exportrun", 2) or ("exportrun", 3);

    /// <summary>A percent-encoded setting value, or null when it is not percent-encoding.</summary>
    private static string? Unescape(string encoded)
    {
        try
        {
            return Uri.UnescapeDataString(encoded);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var parts = message.Substring(Prefix.Length).Split('|');

        for (var i = 1; i < parts.Length; i++)
            if (!IsFree(parts[0], i) && !Names.IsSafe(parts[i])) return new Unknown(message);

        return (parts[0], parts.Length) switch
        {
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            ("close", 1) => new Close(),
            ("load", 2) => new Load(parts[1]),
            ("backup", 2) => new BackUp(parts[1]),
            ("restore", 3) => new Restore(parts[1], parts[2]),
            ("delete", 2) => new DeleteSave(parts[1]),
            ("bringback", 3) => new BringBack(parts[1], parts[2]),
            ("pin", 4) when parts[3] is "0" or "1" => new Pin(parts[1], parts[2], parts[3] == "1"),
            ("remove", 3) => new Remove(parts[1], parts[2]),
            ("folder", 1) => new OpenFolder(),
            ("folder", 2) => new OpenFolder(parts[1]),
            ("exportrun", 4) when parts[2] is "0" or "1" && parts[3] is "0" or "1" => new ExportRun(parts[1], parts[2] == "1", parts[3] == "1"),
            ("importlist", 1) => new ImportList(),
            ("importfile", 2) => new ImportFile(parts[1]),
            ("rename", 3) when Notes.Decode(parts[2]) is { Length: > 0 } name => new Rename(parts[1], name),
            ("runpin", 3) when parts[2] is "0" or "1" => new RunPin(parts[1], parts[2] == "1"),
            ("runnote", 3) when Notes.Decode(parts[2]) is { } text => new SaveNote(parts[1], text),
            ("duplicate", 2) => new Duplicate(parts[1]),
            ("detail", 2) => new Detail(parts[1]),
            ("snapnew", 3) => new SnapshotAsNew(parts[1], parts[2]),
            ("binback", 3) => new BinPutBack(parts[1], parts[2]),
            ("bindel", 3) => new BinDelete(parts[1], parts[2]),
            ("binrundel", 2) => new BinDeleteRun(parts[1]),
            ("binempty", 1) => new BinEmpty(),
            ("set", 3) when SettingKeys.Find(parts[1]) is { } key && Unescape(parts[2]) is { } raw && SettingKeys.Normalize(key, raw) is { } value => new Set(key.Id, value),
            ("newgame", 1) => new NewGame(),
            ("backupnow", 1) => new BackUpNow(),
            ("openinrun", 1) => new OpenInRun(),
            ("gamelist", 1) => new GameList(),
            ("dismiss", 2) => new Dismiss(parts[1]),
            ("copy", 3) => new CopyAsNew(parts[1], parts[2]),
            ("export", 3) => new Export(parts[1], parts[2]),
            ("import", 1) => new Import(),
            ("note", 4) when Notes.Decode(parts[3]) is { } text => new Note(parts[1], parts[2], text),
            ("typing", 2) when parts[1] is "0" or "1" => new Typing(parts[1] == "1"),
            _ => new Unknown(message),
        };
    }
}
