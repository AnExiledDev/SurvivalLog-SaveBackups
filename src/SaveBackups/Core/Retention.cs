using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>
/// How older automatic backups thin out, in days of play: every one from the last <see cref="KeepAllDays"/> in-game
/// days, then the newest of each day for <see cref="DailyDays"/> more, then the newest of each loop when
/// <see cref="PerLoop"/>. <see cref="KeepAllDays"/> of 0 turns thinning off.
/// </summary>
public sealed record Thinning(int KeepAllDays, int DailyDays, bool PerLoop)
{
    public static readonly Thinning Off = new(0, 0, true);

    public bool IsOn => KeepAllDays > 0;
}

/// <summary>How many backups of each kind a run keeps, and the most space all backups may take. 0 means no limit.</summary>
public sealed record Limits(int Morning, int Quit, int Rebirth, int Manual, int Safety, int Expedition, int Wave, int Ending, int Timer, long MaxTotalBytes)
{
    public Thinning Thin { get; init; } = Thinning.Off;

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
        BackupKind.Timer => Timer,
        BackupKind.Imported => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>
/// Which backups to remove. Each run keeps its newest N of each kind, and older automatic backups thin out by days of
/// play (<see cref="Thinning"/>); those go to the recycle bin. Then, while all backups and the bin together take more
/// than the space limit, the oldest go for good, automatic ones before manual and imported ones. Imported backups have
/// no per-kind limit. Pinned backups are never removed and never counted against a kind's N, and the newest backup of
/// each run always stays, so no run is left without one.
/// </summary>
public static class Retention
{
    /// <summary>Everything over the limits, counting and thinning and space together, with no bin.</summary>
    public static IReadOnlyList<BackupMeta> Plan(IReadOnlyList<BackupMeta> all, Limits limits)
    {
        var thinned = Thin(all, limits).ToHashSet();
        var left = all.Where(meta => !thinned.Contains(meta)).ToList();
        var space = OverSpace(left, limits.MaxTotalBytes).ToHashSet();

        return all.Where(meta => thinned.Contains(meta) || space.Contains(meta)).ToList();
    }

    /// <summary>The backups over a kind's N or thinned out by age. These go to the recycle bin.</summary>
    public static IReadOnlyList<BackupMeta> Thin(IReadOnlyList<BackupMeta> all, Limits limits)
    {
        var doomed = new HashSet<BackupMeta>();

        foreach (var group in all.Where(meta => !meta.IsPinned).GroupBy(meta => (meta.Stem, meta.Kind)))
        {
            var keep = limits.For(group.Key.Kind);

            if (keep <= 0) continue;

            foreach (var old in group.OrderByDescending(meta => meta.CreatedUtc).Skip(keep)) doomed.Add(old);
        }

        if (limits.Thin.IsOn)
            foreach (var run in all.GroupBy(meta => meta.Stem))
                foreach (var old in ThinRun(run.ToList(), limits.Thin))
                    doomed.Add(old);

        return all.Where(doomed.Contains).ToList();
    }

    /// <summary>
    /// What must go for <paramref name="kept"/> to fit in <paramref name="maxBytes"/>: oldest first, automatic before
    /// manual and imported, never pinned ones or a run's newest. 0 means no limit.
    /// </summary>
    public static IReadOnlyList<BackupMeta> OverSpace(IReadOnlyList<BackupMeta> kept, long maxBytes)
    {
        if (maxBytes <= 0) return Array.Empty<BackupMeta>();

        var newestOfRun = NewestOfEachRun(kept);
        var total = kept.Sum(meta => meta.Bytes);
        var doomed = new List<BackupMeta>();
        var candidates = kept
            .Where(meta => !meta.IsPinned && !newestOfRun.Contains(meta))
            .OrderBy(meta => meta.Kind is BackupKind.Manual or BackupKind.Imported ? 1 : 0)
            .ThenBy(meta => meta.CreatedUtc);

        foreach (var meta in candidates)
        {
            if (total <= maxBytes) break;

            doomed.Add(meta);
            total -= meta.Bytes;
        }

        return doomed;
    }

    private static HashSet<BackupMeta> NewestOfEachRun(IEnumerable<BackupMeta> all) =>
        all.GroupBy(meta => meta.Stem).Select(run => run.OrderByDescending(meta => meta.CreatedUtc).First()).ToHashSet();

    /// <summary>
    /// One run's automatic backups thinned by age. Age is in in-game days from the run's newest backup, within the
    /// same loop; a backup from an earlier loop is older than any day count.
    /// </summary>
    private static IEnumerable<BackupMeta> ThinRun(IReadOnlyList<BackupMeta> run, Thinning thin)
    {
        var newest = run.OrderByDescending(meta => meta.CreatedUtc).First();
        var (turn, day) = (newest.Run.Progress.Turn, newest.Run.Progress.Day);
        var candidates = run.Where(meta => !meta.IsPinned && meta != newest && BackupKinds.Thins(meta.Kind)).ToList();

        int Age(BackupMeta meta) => meta.Run.Progress.Turn == turn ? Math.Max(0, day - meta.Run.Progress.Day) : int.MaxValue;

        var daily = candidates.Where(meta => Age(meta) >= thin.KeepAllDays && Age(meta) < thin.KeepAllDays + Math.Max(0, thin.DailyDays) && Age(meta) != int.MaxValue);
        var older = candidates.Where(meta => Age(meta) >= thin.KeepAllDays + Math.Max(0, thin.DailyDays)).ToList();

        // Within the daily window, the newest backup of each day of play stays.
        foreach (var dayGroup in daily.GroupBy(meta => (meta.Run.Progress.Turn, meta.Run.Progress.Day)))
            foreach (var old in dayGroup.OrderByDescending(meta => meta.CreatedUtc).Skip(1))
                yield return old;

        if (!thin.PerLoop)
        {
            foreach (var old in older) yield return old;

            yield break;
        }

        // Past it, the newest of each loop stays, unless a newer backup of that loop already stands for it.
        var keptPerLoop = run.Where(meta => !older.Contains(meta)).Select(meta => meta.Run.Progress.Turn).ToHashSet();

        foreach (var loop in older.GroupBy(meta => meta.Run.Progress.Turn))
        {
            var skip = keptPerLoop.Contains(loop.Key) ? 0 : 1;

            foreach (var old in loop.OrderByDescending(meta => meta.CreatedUtc).Skip(skip)) yield return old;
        }
    }
}
