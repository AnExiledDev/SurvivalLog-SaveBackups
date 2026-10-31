using System;

namespace SaveBackups.Core;

/// <summary>Why a backup was made. The name is written into the backup's folder name and its backup.json.</summary>
public enum BackupKind
{
    /// <summary>The start of a new day in a run, right after the game's own morning save.</summary>
    Morning,

    /// <summary>Quit to the title screen or out of the game during a run.</summary>
    Quit,

    /// <summary>Right before a rebirth rolls the run back.</summary>
    Rebirth,

    /// <summary>The player asked for it (the backup key in a run, or Back up now on the title screen).</summary>
    Manual,

    /// <summary>Made by the mod before it restores a backup over a save or deletes a save, so the step can be undone.</summary>
    Safety,

    /// <summary>Right before the survivor sets out on an expedition, after the game saved.</summary>
    Expedition,

    /// <summary>Right before a zombie wave starts, after the game saved.</summary>
    Wave,

    /// <summary>When the run reaches an ending and the game saved the ending choice, so another ending can be tried.</summary>
    Ending,

    /// <summary>Brought in from a .slsave file.</summary>
    Imported,

    /// <summary>Every few minutes of play, while a run is open.</summary>
    Timer,
}

public static class BackupKinds
{
    public static string Id(BackupKind kind) => kind.ToString().ToLowerInvariant();

    public static bool TryParse(string? text, out BackupKind kind) =>
        Enum.TryParse(text, ignoreCase: true, out kind) && Enum.IsDefined(typeof(BackupKind), kind);

    /// <summary>Kinds that record where the run really got to. A safety backup copies whatever was on disk, so it says nothing about that.</summary>
    public static bool ShowsProgress(BackupKind kind) => kind != BackupKind.Safety;

    /// <summary>Kinds the run's own play makes, as opposed to ones the player asked for or the mod made around a change.</summary>
    public static bool IsAutomatic(BackupKind kind) => kind is BackupKind.Morning or BackupKind.Quit or BackupKind.Rebirth or BackupKind.Expedition or BackupKind.Wave or BackupKind.Ending or BackupKind.Timer;

    /// <summary>The kinds made often enough to thin out with age. Rebirths and endings are rare and each one matters, so they keep only their own number.</summary>
    public static bool Thins(BackupKind kind) => kind is BackupKind.Morning or BackupKind.Quit or BackupKind.Expedition or BackupKind.Wave or BackupKind.Timer;
}
