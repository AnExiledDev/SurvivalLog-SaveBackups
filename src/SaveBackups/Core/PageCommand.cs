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

    /// <summary>Open the backup folder in the file explorer.</summary>
    public sealed record OpenFolder : PageCommand;

    /// <summary>Close the manager and open the game's own save list.</summary>
    public sealed record GameList : PageCommand;

    /// <summary>The player read the "save went backwards" warning for this run and wants it gone.</summary>
    public sealed record Dismiss(string Stem) : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var parts = message.Substring(Prefix.Length).Split('|');

        for (var i = 1; i < parts.Length; i++)
            if (!(parts[0] == "pin" && i == 3) && !Names.IsSafe(parts[i])) return new Unknown(message);

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
            ("gamelist", 1) => new GameList(),
            ("dismiss", 2) => new Dismiss(parts[1]),
            _ => new Unknown(message),
        };
    }
}
