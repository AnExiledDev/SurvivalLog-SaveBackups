using System;
using System.Collections.Generic;

namespace SaveBackups.Core;

/// <summary>
/// Spots a run whose save went backwards since the mod last backed it up: the newest backup made while playing is
/// further on than the save the game now lists. That is what an older Steam Cloud copy winning the sync looks like
/// (or a save edited by hand). Rebirth backups are left out, since a rebirth rolls the run back on purpose, and so are
/// safety backups, which copy whatever was on disk. Backups made before the player last dismissed the warning, or
/// before the mod last restored the run, do not count.
/// </summary>
public static class CloudCheck
{
    public static BackupMeta? Behind(Progress listed, IReadOnlyList<BackupMeta> backups, DateTime? settledUtc)
    {
        BackupMeta? newest = null;

        foreach (var meta in backups)
        {
            if (!Counts(meta.Kind)) continue;
            if (settledUtc is { } settled && meta.CreatedUtc <= settled) continue;
            if (newest == null || meta.CreatedUtc > newest.CreatedUtc) newest = meta;
        }

        return newest != null && IsFurther(newest.Run.Progress, listed) ? newest : null;
    }

    /// <summary>
    /// By loop and day only: the time of day the save list keeps can lag the backup's by a few minutes of game time
    /// (the game saves again as the player leaves), which is not a lost save.
    /// </summary>
    private static bool IsFurther(Progress backup, Progress listed) =>
        backup.Turn > listed.Turn || (backup.Turn == listed.Turn && backup.Day > listed.Day);

    private static bool Counts(BackupKind kind) => kind is BackupKind.Morning or BackupKind.Quit or BackupKind.Manual;
}
