using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>
/// The backup folder on disk: one folder per run (named after its save file), and in it one folder per backup holding
/// copies of the run's files and a backup.json. A backup is written into a ".partial" folder and renamed when it is
/// complete, so a crash halfway never leaves a backup that looks whole. A restore copies the backup's files next to
/// the save under temporary names first and only then replaces the save's files, so a failed copy leaves the save as
/// it was. Pure System.IO: the game is never touched here.
/// </summary>
public sealed partial class BackupStore
{
    public const string TempSuffix = ".sbtmp";
    private const string PartialSuffix = ".partial";
    private const string IdFormat = "yyyyMMdd-HHmmss";
    private const string RunNoteFile = "run.json";
    public const string ShotFile = "shot.jpg";

    private readonly string _root;
    private readonly Func<DateTime> _utcNow;

    public BackupStore(string root, Func<DateTime> utcNow)
    {
        _root = root;
        _utcNow = utcNow;
    }

    public string Root => _root;

    public string RunFolder(string stem) => Path.Combine(_root, stem);

    /// <summary>Where exported .slsave files go, inside the backup folder.</summary>
    public string ExportsPath => Path.Combine(_root, ExportsFolder);

    /// <summary>Where .slsave files are put to be imported.</summary>
    public string ImportPath => Path.Combine(_root, ImportFolder);

    public const string ExportsFolder = "Exports";
    public const string ImportFolder = "Import";

    /// <summary>The Exports, Import and Bin folders sit next to the run folders and are not runs.</summary>
    private static bool IsRunFolder(string name) =>
        !string.Equals(name, ExportsFolder, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(name, ImportFolder, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(name, BinFolder, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether any run has a folder of backups.</summary>
    public bool HasRuns() => Directory.Exists(_root) && Directory.EnumerateDirectories(_root).Any(folder => IsRunFolder(Path.GetFileName(folder)));

    /// <summary>
    /// Copies the run's files from <paramref name="saveFolder"/> into a new backup, with <paramref name="shot"/> as its
    /// picture when there is one. With <paramref name="verify"/>, every copy is read back and compared with the save
    /// file before the backup counts as made.
    /// </summary>
    public Result<BackupMeta> Create(string saveFolder, RunFacts run, BackupKind kind, string gameVersion, string dayText = "", byte[]? shot = null, bool verify = false, SaveStats? stats = null)
    {
        var stem = RunFiles.Stem(run.FileName);

        if (!IsSafeId(stem)) return Result.Failed<BackupMeta>("not a save file name: " + run.FileName);

        var files = RunFiles.In(saveFolder, stem);

        if (!files.Any(name => string.Equals(name, RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)))
            return Result.Failed<BackupMeta>("the save file " + RunFiles.MainFile(stem) + " is not in " + saveFolder);

        var created = _utcNow();
        var id = FreeId(stem, created, kind);
        var final = Path.Combine(RunFolder(stem), id);
        var partial = final + PartialSuffix;

        try
        {
            Directory.CreateDirectory(partial);
            long bytes = 0;

            foreach (var name in files)
            {
                var target = Path.Combine(partial, name);

                File.Copy(Path.Combine(saveFolder, name), target, overwrite: true);
                bytes += new FileInfo(target).Length;

                if (verify && !IsSame(Path.Combine(saveFolder, name), target))
                {
                    TryDeleteFolder(partial);

                    return Result.Failed<BackupMeta>("the copy of " + name + " does not match the save; it may have changed while it was copied");
                }
            }

            if (shot is { Length: > 0 }) File.WriteAllBytes(Path.Combine(partial, ShotFile), shot);

            var meta = new BackupMeta(id, stem, kind, created, run, bytes, gameVersion, IsPinned: false) { DayText = dayText ?? "", Stats = stats, HasShot = shot is { Length: > 0 } };

            File.WriteAllText(Path.Combine(partial, BackupMeta.FileName), meta.ToJson());
            Directory.Move(partial, final);

            return Result.Done(meta);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeleteFolder(partial);

            return Result.Failed<BackupMeta>(ex.Message);
        }
    }

    /// <summary>Every complete backup of every run, newest first. Unreadable ones are skipped.</summary>
    public IReadOnlyList<BackupMeta> All()
    {
        if (!Directory.Exists(_root)) return Array.Empty<BackupMeta>();

        return Directory.EnumerateDirectories(_root)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(IsRunFolder)
            .SelectMany(Of)
            .OrderByDescending(meta => meta.CreatedUtc)
            .ThenByDescending(meta => meta.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>One run's complete backups, newest first.</summary>
    public IReadOnlyList<BackupMeta> Of(string stem)
    {
        if (!IsSafeId(stem) || !IsRunFolder(stem)) return Array.Empty<BackupMeta>();

        var folder = RunFolder(stem);

        if (!Directory.Exists(folder)) return Array.Empty<BackupMeta>();

        var found = new List<BackupMeta>();

        foreach (var backup in Directory.EnumerateDirectories(folder))
        {
            var id = Path.GetFileName(backup);

            if (id.EndsWith(PartialSuffix, StringComparison.Ordinal)) continue;

            var metaPath = Path.Combine(backup, BackupMeta.FileName);

            if (!File.Exists(metaPath)) continue;

            try
            {
                var meta = BackupMeta.Parse(id, stem, File.ReadAllText(metaPath));

                if (meta != null) found.Add(meta with { HasShot = File.Exists(Path.Combine(backup, ShotFile)) });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A backup that cannot be read now is left out of the list, not deleted.
            }
        }

        return found.OrderByDescending(meta => meta.CreatedUtc).ThenByDescending(meta => meta.Id, StringComparer.Ordinal).ToList();
    }

    public BackupMeta? Find(string stem, string id) => Of(stem).FirstOrDefault(meta => meta.Id == id);

    /// <summary>
    /// Puts a backup's files in place of the run's files in <paramref name="saveFolder"/>: the backup's copies go in
    /// under temporary names, then the run's present files are deleted, then the copies take their names. Run files
    /// the backup does not have (a snapshot made after it) are removed, so the run is exactly as backed up.
    /// </summary>
    public Result<Unit> Restore(string stem, string id, string saveFolder)
    {
        if (!IsSafeId(stem) || !IsSafeId(id)) return Result.Failed<Unit>("not a backup: " + stem + "/" + id);

        var backup = Path.Combine(RunFolder(stem), id);
        var copies = RunFiles.In(backup, stem);

        if (!copies.Any(name => string.Equals(name, RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)))
            return Result.Failed<Unit>("the backup has no " + RunFiles.MainFile(stem));

        var staged = new List<string>();

        try
        {
            Directory.CreateDirectory(saveFolder);

            foreach (var name in copies)
            {
                var temp = Path.Combine(saveFolder, name + TempSuffix);

                File.Copy(Path.Combine(backup, name), temp, overwrite: true);
                staged.Add(name);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            foreach (var name in staged) TryDeleteFile(Path.Combine(saveFolder, name + TempSuffix));

            return Result.Failed<Unit>(ex.Message);
        }

        try
        {
            foreach (var name in RunFiles.In(saveFolder, stem)) File.Delete(Path.Combine(saveFolder, name));

            // The game's .bak of the main file would be read if the main file failed to load, so it must not be older than the backup.
            TryDeleteFile(Path.Combine(saveFolder, RunFiles.MainFile(stem) + ".bak"));

            foreach (var name in staged) File.Move(Path.Combine(saveFolder, name + TempSuffix), Path.Combine(saveFolder, name), overwrite: true);

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The safety backup made before the restore still holds the run as it was.
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>Moves one backup to the recycle bin.</summary>
    public Result<Unit> Delete(string stem, string id) => MoveToBin(stem, id, BinReason.Deleted);

    /// <summary>Deletes one backup for good, without the recycle bin.</summary>
    public Result<Unit> DeleteForever(string stem, string id)
    {
        if (!IsSafeId(stem) || !IsSafeId(id)) return Result.Failed<Unit>("not a backup: " + stem + "/" + id);

        try
        {
            Directory.Delete(Path.Combine(RunFolder(stem), id), recursive: true);

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>A pinned backup is never removed to make room.</summary>
    public Result<Unit> SetPinned(string stem, string id, bool isPinned)
    {
        var meta = IsSafeId(stem) && IsSafeId(id) ? Find(stem, id) : null;

        if (meta == null) return Result.Failed<Unit>("no backup " + id);

        try
        {
            File.WriteAllText(Path.Combine(RunFolder(stem), id, BackupMeta.FileName), (meta with { IsPinned = isPinned }).ToJson());

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>The player's note on a backup; an empty note removes it.</summary>
    public Result<Unit> SetNote(string stem, string id, string note)
    {
        var meta = IsSafeId(stem) && IsSafeId(id) ? Find(stem, id) : null;

        if (meta == null) return Result.Failed<Unit>("no backup " + id);

        try
        {
            File.WriteAllText(Path.Combine(RunFolder(stem), id, BackupMeta.FileName), (meta with { Note = Notes.Clean(note) }).ToJson());

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>Deletes what a crash left half-written. Run at start, before any backup is made.</summary>
    public int SweepPartials()
    {
        if (!Directory.Exists(_root)) return 0;

        var swept = 0;

        foreach (var run in Directory.EnumerateDirectories(_root))
        foreach (var partial in Directory.EnumerateDirectories(run, "*" + PartialSuffix))
            if (TryDeleteFolder(partial)) swept++;

        return swept;
    }

    /// <summary>Ids and stems are folder names; anything with a path in it is refused, so a page message cannot reach outside the run's folder.</summary>
    private static bool IsSafeId(string id) => Names.IsSafe(id);

    /// <summary>
    /// When the run's save was last put right by the player: a restore, or a dismissed "save went backwards" warning.
    /// Backups older than that are not compared with the save again. Kept in the run folder's run.json.
    /// </summary>
    public DateTime? SettledAt(string stem) => ReadNote(stem).Settled;

    public Result<Unit> Settle(string stem, DateTime utc) => UpdateNote(stem, note => note with { Settled = utc });

    /// <summary>The run's run.json; empty when there is none or it cannot be read.</summary>
    public RunNote ReadNote(string stem)
    {
        if (!IsSafeId(stem) || !IsRunFolder(stem)) return RunNote.Empty;

        var path = Path.Combine(RunFolder(stem), RunNoteFile);

        try
        {
            return File.Exists(path) ? RunNote.Parse(File.ReadAllText(path)) : RunNote.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return RunNote.Empty;
        }
    }

    /// <summary>Changes the run's run.json, written whole through a temp file so a crash never leaves half of one.</summary>
    public Result<Unit> UpdateNote(string stem, Func<RunNote, RunNote> change)
    {
        if (!IsSafeId(stem) || !IsRunFolder(stem)) return Result.Failed<Unit>("not a run: " + stem);

        var path = Path.Combine(RunFolder(stem), RunNoteFile);

        try
        {
            var before = ReadNote(stem);
            var after = change(before);

            if (after == before && File.Exists(path)) return Result.Done(new Unit());

            Directory.CreateDirectory(RunFolder(stem));
            File.WriteAllText(path + TempSuffix, after.ToJson());
            File.Move(path + TempSuffix, path, overwrite: true);

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>Every run folder's run.json, for the save manager.</summary>
    public IReadOnlyDictionary<string, RunNote> AllNotes()
    {
        var notes = new Dictionary<string, RunNote>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_root)) return notes;

        foreach (var folder in Directory.EnumerateDirectories(_root))
        {
            var stem = Path.GetFileName(folder);

            if (!IsRunFolder(stem)) continue;

            var note = ReadNote(stem);

            if (note != RunNote.Empty) notes[stem] = note;
        }

        return notes;
    }

    /// <summary>Every run's settled time.</summary>
    public IReadOnlyDictionary<string, DateTime> AllSettled() =>
        AllNotes().Where(pair => pair.Value.Settled != null).ToDictionary(pair => pair.Key, pair => pair.Value.Settled!.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>The backup's picture file, whether or not it has one.</summary>
    public string ShotPath(string stem, string id) => Path.Combine(RunFolder(stem), id, ShotFile);

    public bool HasShot(string stem, string id) => IsSafeId(stem) && IsSafeId(id) && File.Exists(ShotPath(stem, id));

    /// <summary>Rewrites a backup's backup.json, as with stats read after it was made.</summary>
    public Result<Unit> Rewrite(BackupMeta meta)
    {
        var path = Path.Combine(RunFolder(meta.Stem), meta.Id, BackupMeta.FileName);

        if (!IsSafeId(meta.Stem) || !IsSafeId(meta.Id) || !File.Exists(path)) return Result.Failed<Unit>("no backup " + meta.Id);

        try
        {
            File.WriteAllText(path, meta.ToJson());

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>
    /// Applies <see cref="Retention"/>: backups over a kind's N or thinned by age go to the recycle bin. Then, while
    /// backups and the bin together are over the space limit, the oldest bin entries go for good, then the oldest
    /// backups the space rule picks. Returns how many backups left the list.
    /// </summary>
    public int Prune(Limits limits)
    {
        var removed = 0;

        foreach (var meta in Retention.Thin(All(), limits))
            if (MoveToBin(meta.Stem, meta.Id, BinReason.Thinned) is Result<Unit>.Done) removed++;

        if (limits.MaxTotalBytes <= 0) return removed;

        var kept = All();
        var binBytes = BinBytes();
        var total = kept.Sum(meta => meta.Bytes) + binBytes;

        foreach (var entry in Bin().OrderBy(entry => entry.BinnedUtc))
        {
            if (total <= limits.MaxTotalBytes) break;

            if (DeleteFromBin(entry.Meta.Stem, entry.Meta.Id) is not Result<Unit>.Done) continue;

            total -= entry.Meta.Bytes;
            binBytes -= entry.Meta.Bytes;
        }

        // An empty bin with backups still over the limit: the space rule picks what goes for good. Limit never reaches 0 here, which would mean no limit.
        foreach (var meta in Retention.OverSpace(kept, Math.Max(1, limits.MaxTotalBytes - binBytes)))
            if (DeleteForever(meta.Stem, meta.Id) is Result<Unit>.Done) removed++;

        return removed;
    }

    /// <summary>Whether two files hold the same bytes.</summary>
    private static bool IsSame(string a, string b)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var first = File.OpenRead(a);
        using var second = File.OpenRead(b);

        return first.Length == second.Length && sha.ComputeHash(first).AsSpan().SequenceEqual(sha.ComputeHash(second));
    }

    private string FreeId(string stem, DateTime created, BackupKind kind)
    {
        var baseId = created.ToLocalTime().ToString(IdFormat, CultureInfo.InvariantCulture) + "-" + BackupKinds.Id(kind);
        var id = baseId;

        for (var n = 2; Directory.Exists(Path.Combine(RunFolder(stem), id)) || Directory.Exists(Path.Combine(RunFolder(stem), id + PartialSuffix)); n++) id = baseId + "-" + n;

        return id;
    }

    private static bool TryDeleteFolder(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left for the next restore or the game's own cleanup.
        }
    }
}
