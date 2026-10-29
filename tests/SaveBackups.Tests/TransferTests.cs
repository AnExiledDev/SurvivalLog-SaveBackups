using System.IO.Compression;
using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class TransferTests : IDisposable
{
    private readonly TempDirs _dirs = new();
    private readonly TempDirs _other = new();
    private readonly BackupStore _store;
    private readonly BackupStore _otherStore;

    public TransferTests()
    {
        _store = new BackupStore(_dirs.Backups, () => Make.Start);
        _otherStore = new BackupStore(_other.Backups, () => Make.Start);
    }

    public void Dispose()
    {
        _dirs.Dispose();
        _other.Dispose();
    }

    private BackupMeta Create(BackupKind kind = BackupKind.Morning)
    {
        var made = _store.Create(_dirs.Saves, Make.Run(), kind, "1.1.19611");

        return Assert.IsType<Result<BackupMeta>.Done>(made).Value;
    }

    private string ImportFolder => Path.Combine(_other.Root, "Import");

    // ===== Copy as a new save =====

    [Fact]
    public void A_copy_renames_every_file_to_the_new_run_and_leaves_out_ending_snapshots()
    {
        _dirs.Write("Save_A.bytes", "main");
        _dirs.Write("Save_A.round.bytes", "round");
        _dirs.Write("Save_A.snap2.bytes", "snap");
        _dirs.Write("Save_A.bytes.ending.1", "ending");
        var meta = Create();

        var made = _store.CopyAsNew("Save_A", meta.Id, _dirs.Saves, "Save_B");

        var names = Assert.IsType<Result<IReadOnlyList<string>>.Done>(made).Value.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "Save_B.bytes", "Save_B.round.bytes", "Save_B.snap2.bytes" }, names);
        Assert.Equal("main", _dirs.Read("Save_B.bytes"));
        Assert.Equal("snap", _dirs.Read("Save_B.snap2.bytes"));
        Assert.False(_dirs.Has("Save_B.bytes.ending.1"));
        Assert.Equal("main", _dirs.Read("Save_A.bytes"));
        Assert.Empty(Directory.GetFiles(_dirs.Saves, "*" + BackupStore.TempSuffix));
    }

    [Fact]
    public void A_copy_never_writes_over_a_run_that_has_the_new_name()
    {
        _dirs.Write("Save_A.bytes", "main");
        _dirs.Write("Save_A.round.bytes", "round");
        var meta = Create();
        _dirs.Write("Save_B.round.bytes", "someone else's");

        var made = _store.CopyAsNew("Save_A", meta.Id, _dirs.Saves, "Save_B");

        Assert.IsType<Result<IReadOnlyList<string>>.Failed>(made);
        Assert.False(_dirs.Has("Save_B.bytes"));
        Assert.Equal("someone else's", _dirs.Read("Save_B.round.bytes"));
    }

    [Fact]
    public void A_deleted_saves_backup_can_be_copied()
    {
        _dirs.Write("Save_A.bytes", "main");
        var meta = Create();
        File.Delete(Path.Combine(_dirs.Saves, "Save_A.bytes"));

        var made = _store.CopyAsNew("Save_A", meta.Id, _dirs.Saves, "Save_C");

        Assert.IsType<Result<IReadOnlyList<string>>.Done>(made);
        Assert.Equal("main", _dirs.Read("Save_C.bytes"));
        Assert.False(_dirs.Has("Save_A.bytes"));
    }

    [Fact]
    public void A_new_save_name_follows_the_games_pattern_and_counts_up_when_taken()
    {
        var now = new DateTime(2026, 10, 29, 7, 5, 9);
        var taken = new HashSet<string> { "Save_2026_10_29_07_05_09", "Save_2026_10_29_07_05_09_2" };

        Assert.Equal("Save_2026_10_29_07_05_09", NewSave.FreeStem(now, _ => false));
        Assert.Equal("Save_2026_10_29_07_05_09_3", NewSave.FreeStem(now, taken.Contains));
        Assert.Null(NewSave.FreeStem(now, _ => true));
    }

    [Fact]
    public void Renaming_swaps_only_the_runs_own_stem()
    {
        Assert.Equal("Save_B.snap2.bytes", NewSave.Rename("Save_A.snap2.bytes", "Save_A", "Save_B"));
        Assert.Equal("Save_B.bytes", NewSave.Rename("Save_A.bytes", "Save_A", "Save_B"));
        Assert.Equal("Save_AB.bytes", NewSave.Rename("Save_AB.bytes", "Save_A", "Save_B"));
    }

    // ===== Export and import =====

    [Fact]
    public void An_exported_backup_imports_on_another_computer_as_an_imported_backup()
    {
        _dirs.Write("Save_A.bytes", "main");
        _dirs.Write("Save_A.snap1.bytes", "snap");
        var meta = Create();
        Assert.IsType<Result<Unit>.Done>(_store.SetPinned("Save_A", meta.Id, true));

        var path = Assert.IsType<Result<string>.Done>(_store.Export("Save_A", meta.Id, _store.ExportsPath)).Value;
        Directory.CreateDirectory(ImportFolder);
        File.Copy(path, Path.Combine(ImportFolder, Path.GetFileName(path)));

        var outcome = Assert.Single(_otherStore.ImportAll(ImportFolder));

        Assert.True(outcome.IsDone, outcome.Why);
        var read = Assert.Single(_otherStore.Of("Save_A"));
        Assert.Equal(meta.Id, read.Id);
        Assert.Equal(BackupKind.Imported, read.Kind);
        Assert.False(read.IsPinned);
        Assert.Equal(8, read.Bytes);
        Assert.Equal(meta.Run, read.Run);
        Assert.Equal("snap", File.ReadAllText(Path.Combine(_other.Backups, "Save_A", meta.Id, "Save_A.snap1.bytes")));
        Assert.Empty(Directory.GetFiles(ImportFolder, "*" + BackupStore.ExportExtension));
        Assert.Single(Directory.GetFiles(Path.Combine(ImportFolder, BackupStore.ImportedFolder)));
    }

    [Fact]
    public void Importing_a_backup_already_in_the_library_writes_nothing_and_moves_the_file_aside()
    {
        _dirs.Write("Save_A.bytes", "main");
        var meta = Create();
        var path = Assert.IsType<Result<string>.Done>(_store.Export("Save_A", meta.Id, ImportFolder)).Value;

        var outcome = Assert.Single(_store.ImportAll(ImportFolder));

        Assert.True(outcome.IsAlreadyThere);
        Assert.False(outcome.IsDone);
        Assert.Equal(BackupKind.Morning, Assert.Single(_store.Of("Save_A")).Kind);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void The_export_and_import_folders_are_not_listed_as_runs()
    {
        _dirs.Write("Save_A.bytes", "main");
        var meta = Create();
        _store.Export("Save_A", meta.Id, _store.ExportsPath);
        Directory.CreateDirectory(_store.ImportPath);
        Directory.CreateDirectory(Path.Combine(_store.ImportPath, "x"));

        Assert.Equal(new[] { "Save_A" }, _store.All().Select(m => m.Stem).Distinct().ToArray());
        Assert.Empty(_store.Of(BackupStore.ExportsFolder));
        Assert.Empty(_store.Of(BackupStore.ImportFolder));
    }

    [Fact]
    public void Only_the_export_and_import_folders_do_not_count_as_runs()
    {
        Directory.CreateDirectory(_store.ImportPath);
        Directory.CreateDirectory(_store.ExportsPath);

        Assert.False(_store.HasRuns());

        Directory.CreateDirectory(_store.RunFolder("Save_A"));

        Assert.True(_store.HasRuns());
    }

    private string Zip(string name, params (string Entry, string Text)[] entries)
    {
        Directory.CreateDirectory(ImportFolder);
        var path = Path.Combine(ImportFolder, name + BackupStore.ExportExtension);

        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var (entry, text) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
                writer.Write(text);
            }
        }

        return path;
    }

    private static string MetaJson() => Make.Backup(BackupKind.Morning, 1).ToJson();

    public static IEnumerable<object[]> BadZips() => new[]
    {
        new object[] { "outside", new[] { ("Save_A/id1/backup.json", "M"), ("Save_A/id1/Save_A.bytes", "main"), ("../evil.bytes", "x") } },
        new object[] { "deep", new[] { ("Save_A/id1/backup.json", "M"), ("Save_A/id1/Save_A.bytes", "main"), ("Save_A/id1/sub/Save_A.bytes", "x") } },
        new object[] { "two", new[] { ("Save_A/id1/backup.json", "M"), ("Save_A/id1/Save_A.bytes", "main"), ("Save_A/id2/Save_A.round.bytes", "x") } },
        new object[] { "foreign", new[] { ("Save_A/id1/backup.json", "M"), ("Save_A/id1/Save_A.bytes", "main"), ("Save_A/id1/run.exe", "x") } },
        new object[] { "nomain", new[] { ("Save_A/id1/backup.json", "M"), ("Save_A/id1/Save_A.round.bytes", "round") } },
        new object[] { "nometa", new[] { ("Save_A/id1/Save_A.bytes", "main") } },
        new object[] { "badmeta", new[] { ("Save_A/id1/backup.json", "{ cut"), ("Save_A/id1/Save_A.bytes", "main") } },
    };

    [Theory]
    [MemberData(nameof(BadZips))]
    public void A_file_that_is_not_exactly_one_backup_is_refused_and_left_in_place(string name, (string Entry, string Text)[] entries)
    {
        var path = Zip(name, entries.Select(e => (e.Entry, e.Text == "M" ? MetaJson() : e.Text)).ToArray());

        var outcome = Assert.Single(_otherStore.ImportAll(ImportFolder));

        Assert.False(outcome.IsDone);
        Assert.False(outcome.IsAlreadyThere);
        Assert.NotEqual("", outcome.Why);
        Assert.Empty(_otherStore.All());
        Assert.False(Directory.Exists(Path.Combine(_other.Backups, "Save_A")) && Directory.EnumerateFileSystemEntries(Path.Combine(_other.Backups, "Save_A")).Any());
        Assert.False(File.Exists(Path.Combine(_other.Root, "evil.bytes")));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void A_file_that_is_not_a_zip_is_refused()
    {
        Directory.CreateDirectory(ImportFolder);
        File.WriteAllText(Path.Combine(ImportFolder, "junk" + BackupStore.ExportExtension), "not a zip");

        var outcome = Assert.Single(_otherStore.ImportAll(ImportFolder));

        Assert.False(outcome.IsDone);
        Assert.False(outcome.IsAlreadyThere);
    }

    [Fact]
    public void A_handmade_zip_with_folder_entries_imports()
    {
        Zip("folders", ("Save_A/", ""), ("Save_A/id1/", ""), ("Save_A/id1/backup.json", MetaJson()), ("Save_A/id1/Save_A.bytes", "main"));

        var outcome = Assert.Single(_otherStore.ImportAll(ImportFolder));

        Assert.True(outcome.IsDone, outcome.Why);
        Assert.Equal("id1", Assert.Single(_otherStore.Of("Save_A")).Id);
    }

    // ===== Notes =====

    [Fact]
    public void A_note_is_decoded_cleaned_and_cut()
    {
        Assert.Equal("before the boss | try 2", Notes.Decode(Uri.EscapeDataString("  before the boss | try 2 ")));
        Assert.Equal("a b", Notes.Decode("a%0Ab"));
        Assert.Equal("", Notes.Decode(""));
        Assert.Equal(Notes.MaxLength, Notes.Clean(new string('x', 500)).Length);

        // A cut that would split an emoji drops the whole emoji.
        var emoji = new string('x', Notes.MaxLength - 1) + "\U0001F600";
        Assert.Equal(new string('x', Notes.MaxLength - 1), Notes.Clean(emoji));
    }

    [Fact]
    public void A_note_and_the_days_words_are_kept_on_disk()
    {
        _dirs.Write("Save_A.bytes", "main");
        var made = _store.Create(_dirs.Saves, Make.Run(), BackupKind.Morning, "1.1.19611", "Rain all day.");
        var meta = Assert.IsType<Result<BackupMeta>.Done>(made).Value;

        Assert.IsType<Result<Unit>.Done>(_store.SetNote("Save_A", meta.Id, "before the trader"));

        var read = _store.Find("Save_A", meta.Id)!;
        Assert.Equal("before the trader", read.Note);
        Assert.Equal("Rain all day.", read.DayText);

        Assert.IsType<Result<Unit>.Done>(_store.SetNote("Save_A", meta.Id, ""));
        Assert.Equal("", _store.Find("Save_A", meta.Id)!.Note);
    }
}
