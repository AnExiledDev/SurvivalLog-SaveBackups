using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SaveBackups.Core;

/// <summary>What happened to one file in the Import folder.</summary>
public sealed record ImportOutcome(string File, BackupMeta? Meta, bool IsAlreadyThere, string Why)
{
    public bool IsDone => Meta != null;
}

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
    public Result<string> Export(string stem, string id, string exportFolder)
    {
        if (!IsSafeId(stem) || !IsSafeId(id) || Find(stem, id) == null) return Result.Failed<string>("no backup " + id);

        var backup = Path.Combine(RunFolder(stem), id);
        var path = Path.Combine(exportFolder, stem + "-" + id + ExportExtension);
        var temp = path + TempSuffix;

        try
        {
            Directory.CreateDirectory(exportFolder);
            TryDeleteFile(temp);

            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
            {
                foreach (var name in RunFiles.In(backup, stem).Append(BackupMeta.FileName))
                    zip.CreateEntryFromFile(Path.Combine(backup, name), stem + "/" + id + "/" + name, CompressionLevel.Optimal);
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

    /// <summary>
    /// Adds the backup in one .slsave file to the library, as an Imported backup of its run. The zip must hold
    /// exactly one backup: every entry under the same "stem/id/", only that run's files and a backup.json, the main
    /// file among them, and no more than <see cref="MaxImportBytes"/> in all. A backup with the same id already in
    /// the library is not written again.
    /// </summary>
    public ImportOutcome Import(string zipPath)
    {
        var file = Path.GetFileName(zipPath);

        try
        {
            using var zip = ZipFile.OpenRead(zipPath);

            var check = Check(zip);

            if (check.Why != null) return new ImportOutcome(file, null, false, check.Why);

            var (stem, id) = (check.Stem!, check.Id!);
            var final = Path.Combine(RunFolder(stem), id);

            if (Directory.Exists(final)) return new ImportOutcome(file, null, true, "already in the library");

            var partial = final + PartialSuffix;

            try
            {
                TryDeleteFolder(partial);
                Directory.CreateDirectory(partial);

                foreach (var entry in zip.Entries.Where(entry => entry.Name.Length > 0))
                    entry.ExtractToFile(Path.Combine(partial, entry.Name), overwrite: true);

                var meta = BackupMeta.Parse(id, stem, File.ReadAllText(Path.Combine(partial, BackupMeta.FileName)));

                if (meta == null)
                {
                    TryDeleteFolder(partial);

                    return new ImportOutcome(file, null, false, "its backup.json cannot be read");
                }

                var bytes = RunFiles.In(partial, stem).Sum(name => new FileInfo(Path.Combine(partial, name)).Length);

                meta = meta with { Kind = BackupKind.Imported, Bytes = bytes, IsPinned = false };
                File.WriteAllText(Path.Combine(partial, BackupMeta.FileName), meta.ToJson());
                Directory.Move(partial, final);

                return new ImportOutcome(file, meta, false, "");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                TryDeleteFolder(partial);

                return new ImportOutcome(file, null, false, ex.Message);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new ImportOutcome(file, null, false, "not a backup file: " + ex.Message);
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

    private static (string? Stem, string? Id, string? Why) Check(ZipArchive zip)
    {
        string? stem = null, id = null;
        long total = 0;
        var hasMeta = false;
        var hasMain = false;

        foreach (var entry in zip.Entries)
        {
            var parts = entry.FullName.Replace('\\', '/').Split('/');

            // Folder entries ("stem/" or "stem/id/") carry no data.
            if (entry.Name.Length == 0 && entry.Length == 0 && parts.Length <= 3 && parts[^1].Length == 0) continue;

            if (parts.Length != 3 || !Names.IsSafe(parts[0]) || !Names.IsSafe(parts[1]) || !Names.IsSafe(parts[2]) || entry.Name != parts[2])
                return (null, null, "it holds a file outside one backup: " + entry.FullName);

            stem ??= parts[0];
            id ??= parts[1];

            if (parts[0] != stem || parts[1] != id) return (null, null, "it holds more than one backup");

            if (parts[2] == BackupMeta.FileName) hasMeta = true;
            else if (!RunFiles.Belongs(stem, parts[2])) return (null, null, "it holds a file that is not part of the save: " + parts[2]);

            if (string.Equals(parts[2], RunFiles.MainFile(stem), StringComparison.OrdinalIgnoreCase)) hasMain = true;

            total += entry.Length;

            if (total > MaxImportBytes) return (null, null, "it is too big to be a save");
        }

        if (stem == null || id == null) return (null, null, "it is empty");
        if (!hasMeta) return (null, null, "it has no backup.json");
        if (!hasMain) return (null, null, "it has no " + RunFiles.MainFile(stem));

        return (stem, id, null);
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
