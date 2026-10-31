using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class RetentionTests
{
    private static readonly Limits NoLimits = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    [Fact]
    public void Each_kind_keeps_its_newest_n_per_run()
    {
        var all = new[]
        {
            Make.Backup(BackupKind.Morning, 1),
            Make.Backup(BackupKind.Morning, 2),
            Make.Backup(BackupKind.Morning, 3),
            Make.Backup(BackupKind.Quit, 4),
            Make.Backup(BackupKind.Morning, 5, stem: "Save_B"),
        };

        var doomed = Retention.Plan(all, NoLimits with { Morning = 2 });

        Assert.Equal(new[] { all[0] }, doomed);
    }

    [Fact]
    public void Pinned_backups_stay_and_do_not_count_against_the_limit()
    {
        var pinned = Make.Backup(BackupKind.Morning, 1, pinned: true);
        var older = Make.Backup(BackupKind.Morning, 2);
        var newer = Make.Backup(BackupKind.Morning, 3);

        var doomed = Retention.Plan(new[] { pinned, older, newer }, NoLimits with { Morning = 1 });

        Assert.Equal(new[] { older }, doomed);
    }

    [Fact]
    public void Zero_keeps_every_backup_of_that_kind()
    {
        var all = Enumerable.Range(0, 30).Select(i => Make.Backup(BackupKind.Manual, i)).ToArray();

        Assert.Empty(Retention.Plan(all, NoLimits with { Morning = 1 }));
    }

    [Fact]
    public void The_space_limit_removes_the_oldest_automatic_backups_before_manual_ones()
    {
        var oldManual = Make.Backup(BackupKind.Manual, 1, bytes: 100);
        var oldMorning = Make.Backup(BackupKind.Morning, 2, bytes: 100);
        var midMorning = Make.Backup(BackupKind.Morning, 3, bytes: 100);
        var newest = Make.Backup(BackupKind.Quit, 4, bytes: 100);

        var doomed = Retention.Plan(new[] { oldManual, oldMorning, midMorning, newest }, NoLimits with { MaxTotalBytes = 250 });

        Assert.Equal(new[] { oldMorning, midMorning }, doomed);
    }

    [Fact]
    public void A_run_always_keeps_its_newest_backup_even_over_the_space_limit()
    {
        var only = Make.Backup(BackupKind.Morning, 1, bytes: 10_000);
        var otherRun = Make.Backup(BackupKind.Morning, 2, stem: "Save_B", bytes: 10_000);

        Assert.Empty(Retention.Plan(new[] { only, otherRun }, NoLimits with { MaxTotalBytes = 1 }));
    }
}
