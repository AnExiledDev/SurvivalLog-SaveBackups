using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class CloudCheckTests
{
    private static Progress At(int turn, int day, int time = 0) => new(turn, day, time);

    [Fact]
    public void A_save_behind_its_newest_backup_is_flagged()
    {
        var backup = Make.Backup(BackupKind.Morning, 10, turn: 1, day: 8);

        Assert.Same(backup, CloudCheck.Behind(At(1, 2), new[] { backup }, null));
    }

    [Fact]
    public void An_earlier_loop_is_behind_even_on_a_later_day()
    {
        var backup = Make.Backup(BackupKind.Quit, 10, turn: 2, day: 1);

        Assert.Same(backup, CloudCheck.Behind(At(1, 9), new[] { backup }, null));
    }

    [Fact]
    public void The_same_day_is_not_behind_even_when_the_clock_differs()
    {
        var backup = Make.Backup(BackupKind.Quit, 10, turn: 1, day: 3);

        Assert.Null(CloudCheck.Behind(At(1, 3, 0), new[] { backup }, null));
    }

    [Fact]
    public void Only_the_newest_backup_counts()
    {
        // The player restored an older backup on purpose later; the newest backup is the one the save should match.
        var older = Make.Backup(BackupKind.Morning, 10, day: 8);
        var newer = Make.Backup(BackupKind.Manual, 20, day: 2);

        Assert.Null(CloudCheck.Behind(At(1, 2), new[] { older, newer }, null));
    }

    [Theory]
    [InlineData(BackupKind.Rebirth)]
    [InlineData(BackupKind.Safety)]
    public void Rebirth_and_safety_backups_are_not_compared(BackupKind kind)
    {
        var backup = Make.Backup(kind, 10, day: 8);

        Assert.Null(CloudCheck.Behind(At(1, 2), new[] { backup }, null));
    }

    [Fact]
    public void Backups_from_before_the_run_was_settled_are_ignored()
    {
        var backup = Make.Backup(BackupKind.Morning, 10, day: 8);

        Assert.Null(CloudCheck.Behind(At(1, 2), new[] { backup }, backup.CreatedUtc));
        Assert.Same(backup, CloudCheck.Behind(At(1, 2), new[] { backup }, backup.CreatedUtc.AddMinutes(-1)));
    }
}
