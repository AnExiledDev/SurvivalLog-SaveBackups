using System;
using System.Collections.Generic;

namespace SaveBackups.Core;

/// <summary>
/// Backups that would only repeat one the run already has. The game can save the same morning more than once (a
/// reload, a second pass of the day change), so a morning backup is skipped when the run already has one for that day
/// of that loop; a wave backup likewise, since one day can start more than one wave. An expedition backup is skipped
/// when the run got one on the same day less than <see cref="ExpeditionGap"/> ago, because the game asks to set out
/// again when the first try is refused (a locked map, missing items).
/// </summary>
public static class Repeats
{
    public static readonly TimeSpan ExpeditionGap = TimeSpan.FromMinutes(3);

    public static bool IsRepeat(BackupKind kind, Progress progress, IReadOnlyList<BackupMeta> existing, DateTime utcNow)
    {
        foreach (var meta in existing)
        {
            if (meta.Kind != kind || meta.Run.Progress.Turn != progress.Turn || meta.Run.Progress.Day != progress.Day) continue;

            switch (kind)
            {
                case BackupKind.Morning:
                case BackupKind.Wave:
                    return true;
                case BackupKind.Expedition when utcNow - meta.CreatedUtc < ExpeditionGap:
                    return true;
            }
        }

        return false;
    }
}
