using System.Text.Json;
using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class ModelTests
{
    private static RunView View(string stem, int turn, int day) =>
        new(stem + ".bytes", "Alice", new Progress(turn, day, 0), "Role", "1h", "Loop", "Alive", "Normal", "Day", false, false);

    private static JsonElement Json(Screen screen) => JsonDocument.Parse(PageModel.Json(screen)).RootElement;

    [Fact]
    public void Backups_of_runs_not_in_the_list_show_as_deleted_saves()
    {
        var listed = Make.Backup(BackupKind.Morning, 1, stem: "Save_A");
        var gone = Make.Backup(BackupKind.Quit, 2, stem: "Save_Gone", day: 5);
        var screen = new Screen(new[] { View("Save_A", 1, 3) }, new[] { gone, listed }, new Dictionary<string, DateTime>(), "/b", "F8", false);

        var root = Json(screen);
        var deleted = Assert.Single(root.GetProperty("deleted").EnumerateArray());

        Assert.Equal("Save_Gone", deleted.GetProperty("stem").GetString());
        Assert.Equal(6, deleted.GetProperty("day").GetInt32());
        Assert.Equal(1, root.GetProperty("runs")[0].GetProperty("backups").GetArrayLength());
        Assert.Equal(200, root.GetProperty("bytes").GetInt64());
    }

    [Fact]
    public void A_run_behind_its_backup_carries_the_warning_and_a_settled_one_does_not()
    {
        var ahead = Make.Backup(BackupKind.Morning, 1, stem: "Save_A", day: 8);
        var runs = new[] { View("Save_A", 1, 2) };

        var warned = Json(new Screen(runs, new[] { ahead }, new Dictionary<string, DateTime>(), "/b", "F8", false)).GetProperty("runs")[0];
        var settled = Json(new Screen(runs, new[] { ahead }, new Dictionary<string, DateTime> { ["Save_A"] = ahead.CreatedUtc }, "/b", "F8", false)).GetProperty("runs")[0];

        Assert.Equal(ahead.Id, warned.GetProperty("behind").GetProperty("id").GetString());
        Assert.Equal(9, warned.GetProperty("behind").GetProperty("day").GetInt32());
        Assert.False(settled.TryGetProperty("behind", out _));
    }

    [Fact]
    public void A_morning_backup_of_a_day_already_backed_up_is_a_repeat()
    {
        var existing = new[] { Make.Backup(BackupKind.Morning, 1, turn: 2, day: 4) };

        Assert.True(Repeats.IsRepeat(BackupKind.Morning, new Progress(2, 4, 999), existing));
        Assert.False(Repeats.IsRepeat(BackupKind.Morning, new Progress(2, 5, 0), existing));
        Assert.False(Repeats.IsRepeat(BackupKind.Morning, new Progress(3, 4, 0), existing));
        Assert.False(Repeats.IsRepeat(BackupKind.Manual, new Progress(2, 4, 0), existing));
    }

    [Fact]
    public void Backup_json_round_trips_and_rejects_junk()
    {
        var meta = Make.Backup(BackupKind.Rebirth, 7, turn: 3, day: 11, pinned: true);

        Assert.Equal(meta, BackupMeta.Parse(meta.Id, meta.Stem, meta.ToJson()));
        Assert.Null(BackupMeta.Parse("x", "Save_A", "[]"));
        Assert.Null(BackupMeta.Parse("x", "Save_A", "{\"kind\":\"nope\",\"created\":\"2026-10-01T00:00:00Z\"}"));
        Assert.Null(BackupMeta.Parse("x", "Save_A", "not json"));
    }

    [Theory]
    [InlineData("Save_2026_10_10_01_52_07", true)]
    [InlineData("20261001-080000-morning-2", true)]
    [InlineData("Save_A.bytes", true)]
    [InlineData("", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("a/b", false)]
    [InlineData("a\\b", false)]
    [InlineData("C:", false)]
    public void Only_plain_folder_names_are_safe(string name, bool isSafe)
    {
        Assert.Equal(isSafe, Names.IsSafe(name));
    }

    [Fact]
    public void Run_files_cover_the_snapshots_and_skip_the_games_spares()
    {
        Assert.True(RunFiles.Belongs("Save_A", "Save_A.bytes"));
        Assert.True(RunFiles.Belongs("Save_A", "Save_A.snap5.bytes"));
        Assert.True(RunFiles.Belongs("Save_A", "Save_A.bytes.ending.3"));
        Assert.False(RunFiles.Belongs("Save_A", "Save_A.bytes.bak"));
        Assert.False(RunFiles.Belongs("Save_A", "Save_A.bytes.tmp"));
        Assert.False(RunFiles.Belongs("Save_A", "Save_AB.bytes"));
        Assert.Equal("Save_A", RunFiles.Stem("Save_A.bytes"));
    }
}
