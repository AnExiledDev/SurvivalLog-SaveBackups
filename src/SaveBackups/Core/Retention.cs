using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>How many backups of each kind a run keeps, and the most space all backups may take. 0 means no limit.</summary>
public sealed record Limits(int Morning, int Quit, int Rebirth, int Manual, int Safety, int Expedition, int Wave, int Ending, long MaxTotalBytes)
{
    public int For(BackupKind kind) => kind switch
    {
        BackupKind.Morning => Morning,
        BackupKind.Quit => Quit,
        BackupKind.Rebirth => Rebirth,
        BackupKind.Manual => Manual,
        BackupKind.Safety => Safety,
        BackupKind.Expedition => Expedition,
        BackupKind.Wave => Wave,
        BackupKind.Ending => Ending,
        BackupKind.Imported => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>
/// Which backups to delete. Each run keeps its newest N of each kind. Then, while all backups together take more than
/// the space limit, the oldest goes first, automatic ones before manual and imported ones. Imported backups have no per-kind limit. Pinned backups are never deleted and
/// never counted against a kind's N, and the newest backup of each run always stays, so no run is left without one.
/// </summary>
public static class Retention
{
    public static IReadOnlyList<BackupMeta> Plan(IReadOnlyList<BackupMeta> all, Limits limits)
    {
        var doomed = new HashSet<BackupMeta>();

        foreach (var group in all.Where(meta => !meta.IsPinned).GroupBy(meta => (meta.Stem, meta.Kind)))
        {
            var keep = limits.For(group.Key.Kind);

            if (keep <= 0) continue;

            foreach (var old in group.OrderByDescending(meta => meta.CreatedUtc).Skip(keep)) doomed.Add(old);
        }

        var newestOfRun = all.GroupBy(meta => meta.Stem)
            .Select(run => run.OrderByDescending(meta => meta.CreatedUtc).First())
            .ToHashSet();

        if (limits.MaxTotalBytes > 0)
        {
            var total = all.Where(meta => !doomed.Contains(meta)).Sum(meta => meta.Bytes);
            var candidates = all
                .Where(meta => !meta.IsPinned && !doomed.Contains(meta) && !newestOfRun.Contains(meta))
                .OrderBy(meta => meta.Kind is BackupKind.Manual or BackupKind.Imported ? 1 : 0)
                .ThenBy(meta => meta.CreatedUtc);

            foreach (var meta in candidates)
            {
                if (total <= limits.MaxTotalBytes) break;

                doomed.Add(meta);
                total -= meta.Bytes;
            }
        }

        return all.Where(doomed.Contains).ToList();
    }
}
