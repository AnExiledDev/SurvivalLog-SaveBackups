using System.IO.Compression;
using System.Text.Json;
using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

/// <summary>The save manager's library: the recycle bin, thinning by age, run notes, the second copy, export of a whole run and the page's view of it all.</summary>
public sealed class LibraryTests : IDisposable
{
    private readonly TempDirs _dirs = new();
    private DateTime _now = Make.Start;
    private readonly BackupStore _store;

    public LibraryTests()
    {
        _store = new BackupStore(_dirs.Backups, () => _now);
        _dirs.Write("Save_A.bytes", "main");
    }

    public void Dispose() => _dirs.Dispose();

    private BackupMeta Create(BackupKind kind = BackupKind.Morning, int turn = 1, int day = 3, byte[]? shot = null)
    {
        var made = _store.Create(_dirs.Saves, Make.Run(turn: turn, day: day), kind, "1.1.19611", shot: shot);

        _now = _now.AddMinutes(1);

        return Assert.IsType<Result<BackupMeta>.Done>(made).Value;
    }

    private static Limits NoLimits => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // ===== Recycle bin =====

    [Fact]
    public void A_deleted_backup_waits_in_the_bin_and_can_be_put_back()
    {
        var meta = Create(shot: new byte[] { 1, 2, 3 });

        Assert.IsType<Result<Unit>.Done>(_store.Delete("Save_A", meta.Id));
        Assert.Empty(_store.Of("Save_A"));

        var entry = Assert.Single(_store.Bin());
        Assert.Equal(meta.Id, entry.Meta.Id);
        Assert.Equal(BinReason.Deleted, entry.Why);
        Assert.True(entry.HasShot);

        Assert.IsType<Result<Unit>.Done>(_store.PutBack("Save_A", meta.Id));
        Assert.Empty(_store.Bin());
        Assert.Equal(meta.Id, Assert.Single(_store.Of("Save_A")).Id);
        Assert.True(_store.Of("Save_A")[0].HasShot);
    }

    [Fact]
    public void Pruning_moves_backups_to_the_bin_marked_as_thinned()
    {
        for (var day = 0; day < 3; day++) Create(day: day);

        Assert.Equal(1, _store.Prune(NoLimits with { Morning = 2 }));

        var entry = Assert.Single(_store.Bin());
        Assert.Equal(BinReason.Thinned, entry.Why);
        Assert.Equal(0, entry.Meta.Run.Progress.Day);
    }

    [Fact]
    public void The_bin_lets_go_of_things_only_after_its_days_and_never_when_set_to_zero()
    {
        var old = Create(day: 1);
        var recent = Create(day: 2);
        _store.Delete("Save_A", old.Id);
        _now = _now.AddDays(10);
        _store.Delete("Save_A", recent.Id);
        _now = _now.AddDays(25);

        Assert.Equal(0, _store.ExpireBin(0));
        Assert.Equal(1, _store.ExpireBin(30));
        Assert.Equal(recent.Id, Assert.Single(_store.Bin()).Meta.Id);
    }

    [Fact]
    public void A_deleted_saves_backups_go_after_the_bin_days_but_pinned_ones_stay()
    {
        var pinned = Create(day: 1);
        Create(day: 2);
        _store.SetPinned("Save_A", pinned.Id, true);

        _store.MarkDeleted("Save_A", _now);
        _store.MarkDeleted("Save_A", _now.AddDays(5));
        Assert.Equal(_now, _store.DeletedRuns()["Save_A"]);

        _now = _now.AddDays(31);

        Assert.Equal(1, _store.ExpireBin(30));
        Assert.Equal(pinned.Id, Assert.Single(_store.Of("Save_A")).Id);
    }

    [Fact]
    public void A_save_back_in_the_list_is_no_longer_counted_as_deleted()
    {
        Create();
        _store.MarkDeleted("Save_A", _now);
        _store.MarkListed("Save_A");

        Assert.Empty(_store.DeletedRuns());
    }

    [Fact]
    public void Emptying_the_bin_deletes_the_bin_and_deleted_saves_pinned_or_not()
    {
        var binned = Create(day: 1);
        var pinned = Create(day: 2);
        _store.Delete("Save_A", binned.Id);
        _store.SetPinned("Save_A", pinned.Id, true);
        _store.MarkDeleted("Save_A", _now);

        Assert.Equal(2, _store.EmptyBin());
        Assert.Empty(_store.Bin());
        Assert.Empty(_store.All());
    }

    // ===== Thinning by days of play =====

    [Fact]
    public void Old_days_keep_one_backup_each_and_older_loops_keep_their_newest()
    {
        var oldLoopEarly = Create(BackupKind.Morning, turn: 1, day: 2);
        var oldLoopLate = Create(BackupKind.Quit, turn: 1, day: 9);
        var day3Early = Create(BackupKind.Morning, turn: 2, day: 3);
        var day3Late = Create(BackupKind.Quit, turn: 2, day: 3);
        var recentA = Create(BackupKind.Morning, turn: 2, day: 9);
        var recentB = Create(BackupKind.Quit, turn: 2, day: 9);
        var rebirth = Create(BackupKind.Rebirth, turn: 2, day: 5);
        var newest = Create(BackupKind.Morning, turn: 2, day: 10);

        var limits = NoLimits with { Thin = new Thinning(KeepAllDays: 3, DailyDays: 10, PerLoop: true) };
        var gone = Retention.Thin(_store.All(), limits).Select(meta => meta.Id).ToHashSet();

        // Days 9 and 10 are inside the last 3 days of play: all kept.
        Assert.DoesNotContain(recentA.Id, gone);
        Assert.DoesNotContain(recentB.Id, gone);
        Assert.DoesNotContain(newest.Id, gone);
        // Day 3 is in the daily window: its newest stays.
        Assert.DoesNotContain(day3Late.Id, gone);
        Assert.Contains(day3Early.Id, gone);
        // Loop 1 is past it: its newest stays. Rebirths never thin.
        Assert.DoesNotContain(oldLoopLate.Id, gone);
        Assert.Contains(oldLoopEarly.Id, gone);
        Assert.DoesNotContain(rebirth.Id, gone);
    }

    [Fact]
    public void Without_one_per_loop_everything_past_the_daily_window_goes()
    {
        var oldLoop = Create(BackupKind.Morning, turn: 1, day: 9);
        Create(BackupKind.Morning, turn: 2, day: 10);

        var limits = NoLimits with { Thin = new Thinning(1, 1, PerLoop: false) };

        Assert.Equal(oldLoop.Id, Assert.Single(Retention.Thin(_store.All(), limits)).Id);
    }

    [Fact]
    public void Thinning_off_keeps_everything_the_kind_numbers_allow()
    {
        Create(turn: 1, day: 1);
        Create(turn: 2, day: 10);

        Assert.Empty(Retention.Thin(_store.All(), NoLimits));
    }

    // ===== Run notes =====

    [Fact]
    public void A_runs_star_note_and_play_time_are_kept_on_disk()
    {
        Create();
        _store.UpdateNote("Save_A", note => note with { IsPinned = true, Note = "the good one", PlayedSeconds = 3600 });

        var read = _store.AllNotes()["Save_A"];

        Assert.True(read.IsPinned);
        Assert.Equal("the good one", read.Note);
        Assert.Equal(3600, read.PlayedSeconds);
    }

    [Fact]
    public void A_damaged_run_note_reads_as_empty()
    {
        Assert.Equal(RunNote.Empty, RunNote.Parse("{ not json"));
        Assert.Equal(RunNote.Empty, RunNote.Parse("[1,2]"));
    }

    // ===== Second copy =====

    [Fact]
    public void The_second_copy_follows_the_backups_and_their_pins()
    {
        var second = Path.Combine(_dirs.Root, "Second");
        var kept = Create(day: 1);
        var dropped = Create(day: 2);

        Assert.Equal(2, Assert.IsType<Result<int>.Done>(_store.Mirror(second)).Value);

        _store.Delete("Save_A", dropped.Id);
        _store.SetPinned("Save_A", kept.Id, true);

        Assert.Equal(0, Assert.IsType<Result<int>.Done>(_store.Mirror(second)).Value);
        Assert.False(Directory.Exists(Path.Combine(second, "Save_A", dropped.Id)));
        Assert.True(new BackupStore(second, () => _now).Find("Save_A", kept.Id)!.IsPinned);
    }

    [Fact]
    public void The_second_copy_cannot_be_the_backup_folder_itself()
    {
        Create();

        Assert.IsType<Result<int>.Failed>(_store.Mirror(_dirs.Backups));
        Assert.Equal(0, Assert.IsType<Result<int>.Done>(_store.Mirror("")).Value);
    }

    // ===== Verify =====

    [Fact]
    public void A_verified_backup_is_made_when_the_copy_matches()
    {
        var made = _store.Create(_dirs.Saves, Make.Run(), BackupKind.Manual, "1", verify: true);

        Assert.IsType<Result<BackupMeta>.Done>(made);
    }

    // ===== Export of a whole run =====

    [Fact]
    public void A_whole_run_exports_with_its_save_now_and_every_backup()
    {
        var one = Create(day: 1, shot: new byte[] { 9 });
        var two = Create(day: 2);
        var now = Make.Backup(BackupKind.Manual, 500) with { Id = "now" };

        var path = Assert.IsType<Result<string>.Done>(_store.Export("Save_A", new ExportPick(_dirs.Saves, now, new[] { one.Id, two.Id }, WithExtras: false), _store.ExportsPath, "Alice")).Value;

        using var zip = ZipFile.OpenRead(path);
        var names = zip.Entries.Select(entry => entry.FullName).ToHashSet();

        Assert.Contains("Save_A/now/Save_A.bytes", names);
        Assert.Contains("Save_A/" + one.Id + "/Save_A.bytes", names);
        Assert.Contains("Save_A/" + two.Id + "/backup.json", names);
        Assert.DoesNotContain("Save_A/" + one.Id + "/shot.jpg", names);
    }

    [Fact]
    public void Loose_save_files_in_the_import_folder_are_listed_but_snapshots_are_not()
    {
        Directory.CreateDirectory(_store.ImportPath);
        File.WriteAllText(Path.Combine(_store.ImportPath, "Save_X.bytes"), "x");
        File.WriteAllText(Path.Combine(_store.ImportPath, "Save_X.round.bytes"), "r");
        File.WriteAllText(Path.Combine(_store.ImportPath, "notes.txt"), "n");

        var found = Assert.Single(_store.ImportCandidates());

        Assert.Equal("Save_X.bytes", found.File);
        Assert.True(found.IsLoose);
    }

    [Fact]
    public void A_loose_save_comes_in_under_a_new_name_with_its_round_file()
    {
        Directory.CreateDirectory(_store.ImportPath);
        File.WriteAllText(Path.Combine(_store.ImportPath, "Save_X.bytes"), "x");
        File.WriteAllText(Path.Combine(_store.ImportPath, "Save_X.round.bytes"), "r");

        var names = Assert.IsType<Result<IReadOnlyList<string>>.Done>(_store.CopyLoose("Save_X.bytes", _dirs.Saves, "Save_New")).Value;

        Assert.Contains("Save_New.bytes", names);
        Assert.Equal("r", _dirs.Read("Save_New.round.bytes"));
        Assert.Empty(_store.ImportCandidates());
    }

    // ===== Stats =====

    [Fact]
    public void Stats_round_trip_through_backup_json_and_item_changes_are_biggest_first()
    {
        var then = new SaveStats(new Meter(80, 100), new Meter(50, 100), new Meter(60, 100), new Meter(30, 100), 120, 4, new Dictionary<int, int> { [1] = 5, [2] = 10 }, "Rain");
        var now = then with { Items = new Dictionary<int, int> { [1] = 4, [3] = 7 } };
        var meta = Make.Backup(BackupKind.Morning, 1) with { Stats = then };

        var read = BackupMeta.Parse(meta.Id, meta.Stem, meta.ToJson())!.Stats!;

        Assert.Equal(new Meter(80, 100), read.Health);
        Assert.Equal(120, read.Money);
        Assert.Equal(15, read.ItemTotal);
        Assert.Equal("Rain", read.Journal);
        Assert.Equal(new[] { (2, -10), (3, 7), (1, -1) }, SaveStats.ItemChanges(then, now));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(3600 * 7 + 60 * 5, "07:05")]
    [InlineData(3600 * 23 + 59 * 60 + 59, "23:59")]
    [InlineData(-5, "00:00")]
    public void The_clock_reads_like_the_games(int seconds, string clock) => Assert.Equal(clock, new Progress(1, 1, seconds).Clock);

    // ===== The page's view =====

    private static RunView View(string stem) =>
        new(stem + ".bytes", "Alice", new Progress(1, 3, 3600 * 9), "Role", "1h", "Loop", "Alive", "Normal", "Day", false, false) { SurvivorId = 4 };

    [Fact]
    public void The_view_carries_portraits_stars_notes_pictures_and_stats()
    {
        var stats = new SaveStats(new Meter(80, 100), default, default, default, 5, 2, new Dictionary<int, int> { [1] = 3 }, "");
        var meta = Create(shot: new byte[] { 1 }) with { Stats = stats };
        var screen = new Screen(new[] { View("Save_A") }, new[] { _store.Of("Save_A")[0] with { Stats = stats } }, new Dictionary<string, DateTime>(), "/b", "F8", false)
        {
            Notes = new Dictionary<string, RunNote> { ["Save_A"] = RunNote.Empty with { IsPinned = true, Note = "hi", PlayedSeconds = 90 } },
            Survivors = new Dictionary<int, Survivor> { [4] = new("Ruth", "露丝", "Portrait_Player4.webp", "BackGround_Player4.jpg") },
            Settings = new Dictionary<string, string> { ["Keep.BinDays"] = "30", ["Not.Listed"] = "x" },
            Disk = new Dictionary<string, SaveDisk> { ["Save_A"] = new(4096, _now.AddHours(-2)) },
            NowUtc = _now,
            InRunStem = "Save_A",
        };

        var root = JsonDocument.Parse(PageModel.Json(screen)).RootElement;
        var run = root.GetProperty("runs")[0];
        var backup = run.GetProperty("backups")[0];

        Assert.Equal("Portrait_Player4.webp", root.GetProperty("survivors").GetProperty("4").GetProperty("pic").GetString());
        Assert.Equal(4, run.GetProperty("survivor").GetInt32());
        Assert.True(run.GetProperty("pinned").GetBoolean());
        Assert.Equal("hi", run.GetProperty("note").GetString());
        Assert.Equal(90, run.GetProperty("played").GetInt64());
        Assert.Equal("09:00", run.GetProperty("clock").GetString());
        Assert.True(run.GetProperty("inRun").GetBoolean());
        Assert.Equal(4096, run.GetProperty("saveBytes").GetInt64());
        Assert.Equal(7200, run.GetProperty("lastPlayedAgo").GetInt64());
        Assert.True(backup.GetProperty("shot").GetBoolean());
        Assert.True(backup.GetProperty("auto").GetBoolean());
        Assert.Equal(80, backup.GetProperty("stats").GetProperty("health")[0].GetInt32());
        Assert.Equal(3, backup.GetProperty("stats").GetProperty("items").GetInt32());
        Assert.Equal("30", root.GetProperty("settings").GetProperty("Keep.BinDays").GetString());
        Assert.False(root.GetProperty("settings").TryGetProperty("Not.Listed", out _));
        Assert.Equal(meta.Id, backup.GetProperty("id").GetString());
    }

    [Fact]
    public void Bin_entries_say_how_many_days_they_have_left()
    {
        var meta = Create();
        _store.Delete("Save_A", meta.Id);
        _now = _now.AddDays(10).AddHours(1);

        var screen = new Screen(new[] { View("Save_A") }, Array.Empty<BackupMeta>(), new Dictionary<string, DateTime>(), "/b", "F8", false)
        {
            Bin = _store.Bin(),
            BinDays = 30,
            NowUtc = _now,
        };

        var entry = JsonDocument.Parse(PageModel.Json(screen)).RootElement.GetProperty("bin")[0];

        Assert.Equal(20, entry.GetProperty("daysLeft").GetInt32());
        Assert.Equal("deleted", entry.GetProperty("why").GetString());
        Assert.Equal(-1, PageModel.DaysLeft(_now, 0, _now));
        Assert.Equal(0, PageModel.DaysLeft(_now.AddDays(-40), 30, _now));
    }

    [Fact]
    public void The_detail_lists_items_per_save_with_their_names_and_the_games_snapshots()
    {
        var stats = new SaveStats(default, default, default, default, 0, 0, new Dictionary<int, int> { [7] = 2 }, "");
        var meta = Make.Backup(BackupKind.Morning, 1) with { Stats = stats };
        var detail = new RunDetail(
            "Save_A",
            stats with { Items = new Dictionary<int, int> { [7] = 5 } },
            new[] { meta, Make.Backup(BackupKind.Quit, 2) },
            new[] { new GameSnap("Save_A.snap1.bytes", false, new Progress(1, 4, 0), 100, Make.Start, null) },
            new[] { "file:///photo.jpg" },
            new Dictionary<int, Phrase> { [7] = new("Wood", "木头") });

        var root = JsonDocument.Parse(PageModel.Detail(detail)).RootElement;

        Assert.Equal(5, root.GetProperty("nowItems").GetProperty("7").GetInt32());
        Assert.Equal(2, root.GetProperty("items").GetProperty(meta.Id).GetProperty("7").GetInt32());
        Assert.Equal(1, root.GetProperty("items").EnumerateObject().Count());
        Assert.Equal("木头", root.GetProperty("names").GetProperty("7")[1].GetString());
        Assert.Equal(5, root.GetProperty("snaps")[0].GetProperty("day").GetInt32());
        Assert.Equal("file:///photo.jpg", root.GetProperty("photos")[0].GetString());
    }

    [Fact]
    public void The_import_list_names_the_run_in_each_file_and_marks_the_ones_that_will_not_import()
    {
        var newest = Make.Backup(BackupKind.Quit, 5, stem: "Save_B") with { Run = Make.Run("Save_B", 2, 8, "Bea") };
        var candidates = new[]
        {
            new ImportCandidate("Save_B.slsave", false, 900, Make.Start, newest, 3),
            new ImportCandidate("broken.slsave", false, 10, Make.Start, null, 0),
            new ImportCandidate("Save_C.bytes", true, 50, Make.Start, null, 1),
        };

        var files = JsonDocument.Parse(PageModel.Imports(candidates, "/b/Import")).RootElement.GetProperty("files");

        Assert.Equal("Bea", files[0].GetProperty("name").GetString());
        Assert.Equal(9, files[0].GetProperty("day").GetInt32());
        Assert.Equal(3, files[0].GetProperty("points").GetInt32());
        Assert.True(files[0].GetProperty("readable").GetBoolean());
        Assert.False(files[1].GetProperty("readable").GetBoolean());
        Assert.True(files[2].GetProperty("loose").GetBoolean());
        Assert.True(files[2].GetProperty("readable").GetBoolean());
    }
}
