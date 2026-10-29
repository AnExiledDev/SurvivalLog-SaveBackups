using System.Collections.Generic;

namespace SaveBackups.Core;

/// <summary>
/// The game can save the same morning more than once (a reload, a second pass of the day change), so a morning backup
/// is skipped when the run already has one for that day of that loop.
/// </summary>
public static class Repeats
{
    public static bool IsRepeat(BackupKind kind, Progress progress, IReadOnlyList<BackupMeta> existing)
    {
        if (kind != BackupKind.Morning) return false;

        foreach (var meta in existing)
            if (meta.Kind == BackupKind.Morning && meta.Run.Progress.Turn == progress.Turn && meta.Run.Progress.Day == progress.Day) return true;

        return false;
    }
}
