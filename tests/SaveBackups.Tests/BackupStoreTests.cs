using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class BackupStoreTests : IDisposable
{
    private readonly TempDirs _dirs = new();
    private DateTime _now = Make.Start;
    private readonly BackupStore _store;

    public BackupStoreTests()
    {
        _store = new BackupStore(_dirs.Backups, () => _now);
    }

    public void Dispose() => _dirs.Dispose();

    private BackupMeta Create(BackupKind kind = BackupKind.Morning, int day = 3)
    {
        var made = _store.Create(_dirs.Saves, Make.Run(day: day), kind, "1.1.19611");

        return Assert.IsType<Result<BackupMeta>.Done>(made).Value;
    }

    [Fact]
    public void A_backup_copies_every_file_of_the_run_and_nothing_else()
    {
        _dirs.Write("Save_A.bytes", "main");
        _dirs.Write("Save_A.round.bytes", "round");
        _dirs.Write("Save_A.snap0.bytes", "snap");
        _dirs.Write("Save_A.bytes.bak", "old");
        _dirs.Write("Save_B.bytes", "other run");

        var meta = Create();
        var folder = Path.Combine(_dirs.Backups, "Save_A", meta.Id);
        var copied = Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "Save_A.bytes", "Save_A.round.bytes", "Save_A.snap0.bytes", "backup.json" }, copied);
        Assert.Equal(13, meta.Bytes);
    }

    [Fact]
    public void A_backup_reads_back_with_what_the_run_looked_like()
    {
        _dirs.Write("Save_A.bytes", "main");
        var meta = Create(BackupKind.Quit, day: 9);

        var read = Assert.Single(_store.Of("Save_A"));

        Assert.Equal(meta, read);
        Assert.Equal(9, read.Run.Progress.Day);
        Assert.Equal(BackupKind.Quit, read.Kind);
    }

    [Fact]
    public void Two_backups_in_the_same_second_get_different_ids()
    {
        _dirs.Write("Save_A.bytes", "main");

        var first = Create();
        var second = Create();

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _store.Of("Save_A").Count);
    }

    [Fact]
    public void A_run_without_its_main_file_is_not_backed_up()
    {
        _dirs.Write("Save_A.round.bytes", "round");

        var made = _store.Create(_dirs.Saves, Make.Run(), BackupKind.Morning, "v");

        Assert.IsType<Result<BackupMeta>.Failed>(made);
        Assert.Empty(_store.All());
    }

    [Fact]
    public void Restore_puts_the_backup_back_and_drops_files_made_after_it()
    {
        _dirs.Write("Save_A.bytes", "day 3");
        var meta = Create();

        _dirs.Write("Save_A.bytes", "day 1 from the cloud");
        _dirs.Write("Save_A.snap1.bytes", "later snapshot");
        _dirs.Write("Save_A.bytes.bak", "stale bak");

        var restored = _store.Restore("Save_A", meta.Id, _dirs.Saves);

        Assert.IsType<Result<Unit>.Done>(restored);
        Assert.Equal("day 3", _dirs.Read("Save_A.bytes"));
        Assert.False(_dirs.Has("Save_A.snap1.bytes"));
        Assert.False(_dirs.Has("Save_A.bytes.bak"));
        Assert.Empty(Directory.GetFiles(_dirs.Saves, "*" + BackupStore.TempSuffix));
    }

    [Fact]
    public void Restore_brings_back_a_run_whose_files_are_gone()
    {
        _dirs.Write("Save_A.bytes", "kept");
        var meta = Create();

        File.Delete(Path.Combine(_dirs.Saves, "Save_A.bytes"));

        Assert.IsType<Result<Unit>.Done>(_store.Restore("Save_A", meta.Id, _dirs.Saves));
        Assert.Equal("kept", _dirs.Read("Save_A.bytes"));
    }

    [Theory]
    [InlineData("..", "x")]
    [InlineData("Save_A", "..")]
    [InlineData("Save_A", "../../Saves")]
    [InlineData("Save_A/..", "x")]
    [InlineData("C:", "x")]
    public void Paths_outside_the_backup_folder_are_refused(string stem, string id)
    {
        _dirs.Write("Save_A.bytes", "main");
        Create();

        Assert.IsType<Result<Unit>.Failed>(_store.Restore(stem, id, _dirs.Saves));
        Assert.IsType<Result<Unit>.Failed>(_store.Delete(stem, id));
        Assert.True(Directory.Exists(_dirs.Saves));
        Assert.Equal("main", _dirs.Read("Save_A.bytes"));
    }

    [Fact]
    public void A_half_written_backup_is_never_listed_and_is_swept_at_start()
    {
        _dirs.Write("Save_A.bytes", "main");
        Create();
        var partial = Path.Combine(_dirs.Backups, "Save_A", "20261001-090000-morning.partial");
        Directory.CreateDirectory(partial);
        File.WriteAllText(Path.Combine(partial, BackupMeta.FileName), Make.Backup(BackupKind.Morning, 60).ToJson());

        Assert.Single(_store.Of("Save_A"));
        Assert.Equal(1, _store.SweepPartials());
        Assert.False(Directory.Exists(partial));
        Assert.Single(_store.Of("Save_A"));
    }

    [Fact]
    public void A_broken_backup_json_hides_only_that_backup()
    {
        _dirs.Write("Save_A.bytes", "main");
        var good = Create();
        _now = _now.AddMinutes(1);
        var bad = Create();
        File.WriteAllText(Path.Combine(_dirs.Backups, "Save_A", bad.Id, BackupMeta.FileName), "{ cut sho");

        Assert.Equal(good.Id, Assert.Single(_store.Of("Save_A")).Id);
    }

    [Fact]
    public void Pinning_is_kept_on_disk()
    {
        _dirs.Write("Save_A.bytes", "main");
        var meta = Create();

        Assert.IsType<Result<Unit>.Done>(_store.SetPinned("Save_A", meta.Id, true));
        Assert.True(_store.Find("Save_A", meta.Id)!.IsPinned);
    }

    [Fact]
    public void Settled_time_round_trips_for_each_run()
    {
        var at = Make.Start.AddHours(5);

        _store.Settle("Save_A", at);

        Assert.Equal(at, _store.SettledAt("Save_A"));
        Assert.Equal(at, _store.AllSettled()["Save_A"]);
        Assert.Null(_store.SettledAt("Save_B"));
    }

    [Fact]
    public void Prune_deletes_what_is_over_the_limits()
    {
        _dirs.Write("Save_A.bytes", "main");

        for (var i = 0; i < 4; i++)
        {
            _now = Make.Start.AddMinutes(i);
            Create(day: i);
        }

        var removed = _store.Prune(new Limits(2, 0, 0, 0, 0, 0, 0, 0, 0));

        Assert.Equal(2, removed);
        Assert.Equal(new[] { 3, 2 }, _store.Of("Save_A").Select(m => m.Run.Progress.Day).ToArray());
    }
}
