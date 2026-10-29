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

    /// <summary>The Exports and Import folders sit next to the run folders and are not runs.</summary>
    private static bool IsRunFolder(string name) =>
        !string.Equals(name, ExportsFolder, StringComparison.OrdinalIgnoreCase) && !string.Equals(name, ImportFolder, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether any run has a folder of backups.</summary>
    public bool HasRuns() => Directory.Exists(_root) && Directory.EnumerateDirectories(_root).Any(folder => IsRunFolder(Path.GetFileName(folder)));

    /// <summary>Copies the run's files from <paramref name="saveFolder"/> into a new backup.</summary>
    public Result<BackupMeta> Create(string saveFolder, RunFacts run, BackupKind kind, string gameVersion, string dayText = "")
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
            }

            var meta = new BackupMeta(id, stem, kind, created, run, bytes, gameVersion, IsPinned: false) { DayText = dayText ?? "" };

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

                if (meta != null) found.Add(meta);
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

    public Result<Unit> Delete(string stem, string id)
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
    public DateTime? SettledAt(string stem)
    {
        var path = Path.Combine(RunFolder(stem), RunNoteFile);

        try
        {
            if (!File.Exists(path)) return null;

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));

            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                   && document.RootElement.TryGetProperty("settled", out var value)
                   && value.ValueKind == System.Text.Json.JsonValueKind.String
                   && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at)
                ? at.ToUniversalTime()
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public Result<Unit> Settle(string stem, DateTime utc)
    {
        if (!IsSafeId(stem)) return Result.Failed<Unit>("not a run: " + stem);

        try
        {
            Directory.CreateDirectory(RunFolder(stem));
            File.WriteAllText(Path.Combine(RunFolder(stem), RunNoteFile), "{\"settled\":\"" + utc.ToString("o", CultureInfo.InvariantCulture) + "\"}");

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>Every run's settled time, for the save manager.</summary>
    public IReadOnlyDictionary<string, DateTime> AllSettled()
    {
        var settled = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_root)) return settled;

        foreach (var folder in Directory.EnumerateDirectories(_root))
        {
            var stem = Path.GetFileName(folder);

            if (SettledAt(stem) is { } at) settled[stem] = at;
        }

        return settled;
    }

    /// <summary>Deletes what <see cref="Retention"/> says is over the limits; returns how many went.</summary>
    public int Prune(Limits limits)
    {
        var removed = 0;

        foreach (var meta in Retention.Plan(All(), limits))
            if (Delete(meta.Stem, meta.Id) is Result<Unit>.Done) removed++;

        return removed;
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
