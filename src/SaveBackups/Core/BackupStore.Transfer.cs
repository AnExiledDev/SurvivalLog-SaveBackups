using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>What happened to one file in the Import folder. <see cref="Meta"/> is the newest backup it held.</summary>
public sealed record ImportOutcome(string File, BackupMeta? Meta, bool IsAlreadyThere, string Why)
{
    public bool IsDone => Meta != null && !IsAlreadyThere;

    /// <summary>How many backups were added.</summary>
    public int Count { get; init; }
}

/// <summary>A file in the Import folder that can be added: a .slsave, or a loose save file from another computer.</summary>
public sealed record ImportCandidate(string File, bool IsLoose, long Bytes, DateTime WrittenUtc, BackupMeta? Newest, int Points);

/// <summary>
/// What goes into an export: the run's save as it is now (its files and a backup.json made from what the game lists),
/// the backups named, and with <see cref="WithExtras"/> their pictures and notes.
/// </summary>
public sealed record ExportPick(string? SaveFolder, BackupMeta? Now, IReadOnlyList<string> Ids, bool WithExtras);

/// <summary>Copying a backup out as a new save, and moving backups between computers as .slsave files (zips).</summary>
public sealed partial class BackupStore
{
    public const string ExportExtension = ".slsave";
    public const string ImportedFolder = "Imported";

    /// <summary>Bigger than any real run (a long run is a few MB); a zip past this is refused before anything is written.</summary>
    public const long MaxImportBytes = 512L * 1024 * 1024;

    /// <summary>
    /// Copies a backup's files into <paramref name="saveFolder"/> as the new run <paramref name="newStem"/>, renamed
    /// to match, and gives the names written. Ending snapshots are left out (<see cref="NewSave.IsCopied"/>). Nothing
    /// is overwritten: if any target exists, nothing is written. The main file still names the old run inside it;
    /// the caller must rewrite that before the game loads it, or the game would save over the old run.
    /// </summary>
    public Result<IReadOnlyList<string>> CopyAsNew(string stem, string id, string saveFolder, string newStem)
    {
        if (!IsSafeId(stem) || !IsSafeId(id) || !IsSafeId(newStem)) return Result.Failed<IReadOnlyList<string>>("not a backup: " + stem + "/" + id);

        var backup = Path.Combine(RunFolder(stem), id);
        var copies = RunFiles.In(backup, stem).Where(name => NewSave.IsCopied(stem, name)).ToList();

        if (!copies.Any(name => string.Equals(name, RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)))
            return Result.Failed<IReadOnlyList<string>>("the backup has no " + RunFiles.MainFile(stem));

        var targets = copies.Select(name => NewSave.Rename(name, stem, newStem)).ToList();

        if (RunFiles.In(saveFolder, newStem).Count > 0 || targets.Any(name => File.Exists(Path.Combine(saveFolder, name))))
            return Result.Failed<IReadOnlyList<string>>("a save named " + newStem + " already exists");

        var staged = new List<string>();

        try
        {
            Directory.CreateDirectory(saveFolder);

            for (var i = 0; i < copies.Count; i++)
            {
                File.Copy(Path.Combine(backup, copies[i]), Path.Combine(saveFolder, targets[i] + TempSuffix), overwrite: true);
                staged.Add(targets[i]);
            }

            foreach (var name in staged) File.Move(Path.Combine(saveFolder, name + TempSuffix), Path.Combine(saveFolder, name));

            return Result.Done<IReadOnlyList<string>>(targets);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            foreach (var name in staged)
            {
                TryDeleteFile(Path.Combine(saveFolder, name + TempSuffix));
                TryDeleteFile(Path.Combine(saveFolder, name));
            }

            return Result.Failed<IReadOnlyList<string>>(ex.Message);
        }
    }

    /// <summary>
    /// Writes a backup as one .slsave file in <paramref name="exportFolder"/> and gives its path. Inside, every file
    /// sits under "stem/id/", so an import knows which run and backup it is.
    /// </summary>
    public Result<string> Export(string stem, string id, string exportFolder) =>
        Export(stem, new ExportPick(null, null, new[] { id }, WithExtras: true), exportFolder, stem + "-" + id);

    /// <summary>
    /// Writes a run's save and backups as one .slsave file named <paramref name="name"/> in
    /// <paramref name="exportFolder"/> and gives its path. Inside, every file sits under "stem/id/", one folder per
    /// backup; the save as it is now is one more backup there.
    /// </summary>
    public Result<string> Export(string stem, ExportPick pick, string exportFolder, string name)
    {
        if (!IsSafeId(stem) || !IsSafeId(name)) return Result.Failed<string>("not a run: " + stem);

        var backups = pick.Ids.Select(id => IsSafeId(id) ? Find(stem, id) : null).ToList();

        if (backups.Any(meta => meta == null)) return Result.Failed<string>("a backup is missing");
        if (backups.Count == 0 && pick.Now == null) return Result.Failed<string>("nothing to export");

        var path = Path.Combine(exportFolder, name + ExportExtension);
        var temp = path + TempSuffix;

        try
        {
            Directory.CreateDirectory(exportFolder);
            TryDeleteFile(temp);

            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
            {
                foreach (var meta in backups.OfType<BackupMeta>())
                {
                    var folder = Path.Combine(RunFolder(stem), meta.Id);
                    var shown = pick.WithExtras ? meta : meta with { Note = "" };

                    foreach (var file in RunFiles.In(folder, stem))
                        zip.CreateEntryFromFile(Path.Combine(folder, file), stem + "/" + meta.Id + "/" + file, CompressionLevel.Optimal);

                    WriteEntry(zip, stem + "/" + meta.Id + "/" + BackupMeta.FileName, shown.ToJson());

                    if (pick.WithExtras && File.Exists(Path.Combine(folder, ShotFile)))
                        zip.CreateEntryFromFile(Path.Combine(folder, ShotFile), stem + "/" + meta.Id + "/" + ShotFile, CompressionLevel.Optimal);
                }

                if (pick.Now is { } now && pick.SaveFolder != null && IsSafeId(now.Id))
                {
                    var files = RunFiles.In(pick.SaveFolder, stem).Where(file => NewSave.IsCopied(stem, file)).ToList();

                    if (!files.Contains(RunFiles.MainFile(stem), StringComparer.OrdinalIgnoreCase)) throw new IOException("the save file " + RunFiles.MainFile(stem) + " is missing");

                    foreach (var file in files)
                        zip.CreateEntryFromFile(Path.Combine(pick.SaveFolder, file), stem + "/" + now.Id + "/" + file, CompressionLevel.Optimal);

                    var bytes = files.Sum(file => new FileInfo(Path.Combine(pick.SaveFolder, file)).Length);

                    WriteEntry(zip, stem + "/" + now.Id + "/" + BackupMeta.FileName, (now with { Bytes = bytes }).ToJson());
                }
            }

            File.Move(temp, path, overwrite: true);

            return Result.Done(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            TryDeleteFile(temp);

            return Result.Failed<string>(ex.Message);
        }
    }

    private static void WriteEntry(ZipArchive zip, string name, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open());

        writer.Write(text);
    }

    /// <summary>
    /// Adds the backups in one .slsave file to the library, as Imported backups of their run. The zip must hold one
    /// run: every entry under "stem/id/", only that run's files, a backup.json and a picture, the main file in each
    /// backup, and no more than <see cref="MaxImportBytes"/> in all. A backup with an id already in the library is
    /// not written again.
    /// </summary>
    public ImportOutcome Import(string zipPath)
    {
        var file = Path.GetFileName(zipPath);

        try
        {
            using var zip = ZipFile.OpenRead(zipPath);

            var check = Check(zip);

            if (check.Why != null) return new ImportOutcome(file, null, false, check.Why);

            var stem = check.Stem!;
            var added = new List<BackupMeta>();

            foreach (var id in check.Ids!)
            {
                var final = Path.Combine(RunFolder(stem), id);

                if (Directory.Exists(final)) continue;

                var partial = final + PartialSuffix;

                try
                {
                    TryDeleteFolder(partial);
                    Directory.CreateDirectory(partial);

                    foreach (var entry in zip.Entries.Where(entry => entry.Name.Length > 0 && entry.FullName.Replace('\\', '/').StartsWith(stem + "/" + id + "/", StringComparison.Ordinal)))
                        entry.ExtractToFile(Path.Combine(partial, entry.Name), overwrite: true);

                    var meta = BackupMeta.Parse(id, stem, File.ReadAllText(Path.Combine(partial, BackupMeta.FileName)));

                    if (meta == null)
                    {
                        TryDeleteFolder(partial);

                        return new ImportOutcome(file, null, false, "its backup.json cannot be read") { Count = added.Count };
                    }

                    var bytes = RunFiles.In(partial, stem).Sum(name => new FileInfo(Path.Combine(partial, name)).Length);

                    meta = meta with { Kind = BackupKind.Imported, Bytes = bytes, IsPinned = false };
                    File.WriteAllText(Path.Combine(partial, BackupMeta.FileName), meta.ToJson());
                    Directory.Move(partial, final);
                    added.Add(meta);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    TryDeleteFolder(partial);

                    return new ImportOutcome(file, null, false, ex.Message) { Count = added.Count };
                }
            }

            if (added.Count == 0) return new ImportOutcome(file, Of(stem).FirstOrDefault(), true, "already in the library");

            return new ImportOutcome(file, added.OrderByDescending(meta => meta.CreatedUtc).First(), false, "") { Count = added.Count };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new ImportOutcome(file, null, false, "not a backup file: " + ex.Message);
        }
    }

    /// <summary>The .slsave files and loose save files (Save_*.bytes) in the Import folder, newest first.</summary>
    public IReadOnlyList<ImportCandidate> ImportCandidates()
    {
        if (!Directory.Exists(ImportPath)) return Array.Empty<ImportCandidate>();

        var found = new List<ImportCandidate>();

        foreach (var path in Directory.EnumerateFiles(ImportPath))
        {
            var name = Path.GetFileName(path);
            var info = new FileInfo(path);

            if (name.EndsWith(ExportExtension, StringComparison.OrdinalIgnoreCase))
            {
                var (newest, points) = Peek(path);

                found.Add(new ImportCandidate(name, false, info.Length, info.LastWriteTimeUtc, newest, points));
            }
            else if (IsLooseSave(name))
            {
                found.Add(new ImportCandidate(name, true, info.Length, info.LastWriteTimeUtc, null, 1));
            }
        }

        return found.OrderByDescending(candidate => candidate.WrittenUtc).ToList();
    }

    /// <summary>A save's main file: Save_X.bytes, not one of its snapshots.</summary>
    public static bool IsLooseSave(string name) =>
        Names.IsSafe(name)
        && name.StartsWith("Save_", StringComparison.OrdinalIgnoreCase)
        && name.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase)
        && RunFiles.Stem(name).IndexOf('.') < 0;

    /// <summary>
    /// Copies a loose save from the Import folder into <paramref name="saveFolder"/> as <paramref name="newStem"/>,
    /// with the round and snapshot files beside it, and gives the names written. Nothing is overwritten.
    /// </summary>
    public Result<IReadOnlyList<string>> CopyLoose(string fileName, string saveFolder, string newStem)
    {
        if (!IsLooseSave(fileName) || !IsSafeId(newStem)) return Result.Failed<IReadOnlyList<string>>("not a save file: " + fileName);

        var stem = RunFiles.Stem(fileName);
        var files = RunFiles.In(ImportPath, stem).Where(name => NewSave.IsCopied(stem, name)).ToList();

        if (!files.Contains(fileName, StringComparer.OrdinalIgnoreCase)) return Result.Failed<IReadOnlyList<string>>("no " + fileName + " in the Import folder");

        var targets = files.Select(name => NewSave.Rename(name, stem, newStem)).ToList();

        if (RunFiles.In(saveFolder, newStem).Count > 0) return Result.Failed<IReadOnlyList<string>>("a save named " + newStem + " already exists");

        try
        {
            Directory.CreateDirectory(saveFolder);

            for (var i = 0; i < files.Count; i++) File.Copy(Path.Combine(ImportPath, files[i]), Path.Combine(saveFolder, targets[i]));

            foreach (var name in files) MoveAside(Path.Combine(ImportPath, name), Path.Combine(ImportPath, ImportedFolder));

            return Result.Done<IReadOnlyList<string>>(targets);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            foreach (var name in targets) TryDeleteFile(Path.Combine(saveFolder, name));

            return Result.Failed<IReadOnlyList<string>>(ex.Message);
        }
    }

    /// <summary>Imports one .slsave from the Import folder and moves it aside when it was added or was already there.</summary>
    public ImportOutcome ImportFile(string fileName)
    {
        if (!Names.IsSafe(fileName) || !fileName.EndsWith(ExportExtension, StringComparison.OrdinalIgnoreCase))
            return new ImportOutcome(fileName, null, false, "not a .slsave file");

        var path = Path.Combine(ImportPath, fileName);

        if (!File.Exists(path)) return new ImportOutcome(fileName, null, false, "it is not in the Import folder");

        var outcome = Import(path);

        if (outcome.IsDone || outcome.IsAlreadyThere) MoveAside(path, Path.Combine(ImportPath, ImportedFolder));

        return outcome;
    }

    /// <summary>The newest backup.json in a .slsave and how many backups it holds, without unpacking it.</summary>
    private static (BackupMeta? Newest, int Points) Peek(string zipPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var check = Check(zip);

            if (check.Why != null) return (null, 0);

            BackupMeta? newest = null;

            foreach (var id in check.Ids!)
            {
                var entry = zip.GetEntry(check.Stem + "/" + id + "/" + BackupMeta.FileName);

                if (entry == null) continue;

                using var reader = new StreamReader(entry.Open());
                var meta = BackupMeta.Parse(id, check.Stem!, reader.ReadToEnd());

                if (meta != null && (newest == null || meta.CreatedUtc > newest.CreatedUtc)) newest = meta;
            }

            return (newest, check.Ids!.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return (null, 0);
        }
    }

    /// <summary>
    /// Imports every .slsave file in <paramref name="importFolder"/>. One that was added, or was already in the
    /// library, moves to its Imported folder so it is not read again; one that failed stays where it is.
    /// </summary>
    public IReadOnlyList<ImportOutcome> ImportAll(string importFolder)
    {
        if (!Directory.Exists(importFolder)) return Array.Empty<ImportOutcome>();

        var outcomes = new List<ImportOutcome>();

        foreach (var path in Directory.EnumerateFiles(importFolder, "*" + ExportExtension).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList())
        {
            var outcome = Import(path);

            if (outcome.IsDone || outcome.IsAlreadyThere) MoveAside(path, Path.Combine(importFolder, ImportedFolder));

            outcomes.Add(outcome);
        }

        return outcomes;
    }

    private static (string? Stem, IReadOnlyList<string>? Ids, string? Why) Check(ZipArchive zip)
    {
        string? stem = null;
        var ids = new List<string>();
        var hasMeta = new HashSet<string>(StringComparer.Ordinal);
        var hasMain = new HashSet<string>(StringComparer.Ordinal);
        long total = 0;

        foreach (var entry in zip.Entries)
        {
            var parts = entry.FullName.Replace('\\', '/').Split('/');

            // Folder entries ("stem/" or "stem/id/") carry no data.
            if (entry.Name.Length == 0 && entry.Length == 0 && parts.Length <= 3 && parts[^1].Length == 0) continue;

            if (parts.Length != 3 || !Names.IsSafe(parts[0]) || !Names.IsSafe(parts[1]) || !Names.IsSafe(parts[2]) || entry.Name != parts[2])
                return (null, null, "it holds a file outside one backup: " + entry.FullName);

            stem ??= parts[0];

            if (parts[0] != stem) return (null, null, "it holds more than one save");

            if (!ids.Contains(parts[1])) ids.Add(parts[1]);

            if (parts[2] == BackupMeta.FileName) hasMeta.Add(parts[1]);
            else if (parts[2] != ShotFile && !RunFiles.Belongs(stem, parts[2])) return (null, null, "it holds a file that is not part of the save: " + parts[2]);

            if (string.Equals(parts[2], RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)) hasMain.Add(parts[1]);

            total += entry.Length;

            if (total > MaxImportBytes) return (null, null, "it is too big to be a save");
        }

        if (stem == null || ids.Count == 0) return (null, null, "it is empty");
        if (ids.Any(id => !hasMeta.Contains(id))) return (null, null, "it has no backup.json");
        if (ids.Any(id => !hasMain.Contains(id))) return (null, null, "it has no " + RunFiles.MainFile(stem));

        return (stem, ids, null);
    }

    private static void MoveAside(string path, string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);

            var target = Path.Combine(folder, Path.GetFileName(path));

            for (var n = 2; File.Exists(target); n++)
                target = Path.Combine(folder, Path.GetFileNameWithoutExtension(path) + "-" + n + Path.GetExtension(path));

            File.Move(path, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left in place; the next import finds it already in the library and tries again to move it.
        }
    }
}
