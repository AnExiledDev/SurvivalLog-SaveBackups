using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SaveBackups.Core;

/// <summary>Why a backup is in the recycle bin.</summary>
public enum BinReason
{
    /// <summary>The player deleted it.</summary>
    Deleted,

    /// <summary>It was over its kind's number, or thinned out by age.</summary>
    Thinned,
}

/// <summary>A backup in the recycle bin and when it went there.</summary>
public sealed record BinEntry(BackupMeta Meta, DateTime BinnedUtc, BinReason Why, bool HasShot);

/// <summary>
/// The recycle bin: Bin/&lt;stem&gt;/&lt;id&gt;/ under the backup folder, each with a binned.json saying when and why.
/// A deleted save is not moved; its run.json gets the time it left the game's list, and its backups wait in place
/// for the same number of days. Also the second copy of the backups in another folder.
/// </summary>
public sealed partial class BackupStore
{
    public const string BinFolder = "Bin";
    private const string BinnedFile = "binned.json";

    public string BinPath => Path.Combine(_root, BinFolder);

    private string BinItem(string stem, string id) => Path.Combine(BinPath, stem, id);

    public Result<Unit> MoveToBin(string stem, string id, BinReason why)
    {
        if (!IsSafeId(stem) || !IsSafeId(id)) return Result.Failed<Unit>("not a backup: " + stem + "/" + id);

        var source = Path.Combine(RunFolder(stem), id);

        if (!Directory.Exists(source)) return Result.Failed<Unit>("no backup " + id);

        try
        {
            var target = BinItem(stem, id);

            // A backup put back and binned again replaces its older copy in the bin.
            TryDeleteFolder(target);
            Directory.CreateDirectory(Path.Combine(BinPath, stem));
            Directory.Move(source, target);
            File.WriteAllText(Path.Combine(target, BinnedFile), "{\"at\":\"" + _utcNow().ToString("o", CultureInfo.InvariantCulture) + "\",\"why\":\"" + (why == BinReason.Thinned ? "thinned" : "deleted") + "\"}");

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    /// <summary>Every backup in the bin, most recently binned first.</summary>
    public IReadOnlyList<BinEntry> Bin()
    {
        if (!Directory.Exists(BinPath)) return Array.Empty<BinEntry>();

        var entries = new List<BinEntry>();

        foreach (var run in Directory.EnumerateDirectories(BinPath))
        foreach (var item in Directory.EnumerateDirectories(run))
        {
            var (stem, id) = (Path.GetFileName(run), Path.GetFileName(item));

            try
            {
                var meta = BackupMeta.Parse(id, stem, File.ReadAllText(Path.Combine(item, BackupMeta.FileName)));

                if (meta == null) continue;

                var (at, why) = ReadBinned(Path.Combine(item, BinnedFile));

                entries.Add(new BinEntry(meta, at ?? Directory.GetLastWriteTimeUtc(item), why, File.Exists(Path.Combine(item, ShotFile))));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Unreadable now; it stays and is listed once it can be read.
            }
        }

        return entries.OrderByDescending(entry => entry.BinnedUtc).ToList();
    }

    public long BinBytes() => Bin().Sum(entry => entry.Meta.Bytes);

    public string BinShotPath(string stem, string id) => Path.Combine(BinItem(stem, id), ShotFile);

    /// <summary>Moves a backup out of the bin, back into its run's list.</summary>
    public Result<Unit> PutBack(string stem, string id)
    {
        if (!IsSafeId(stem) || !IsSafeId(id)) return Result.Failed<Unit>("not a backup: " + stem + "/" + id);

        var source = BinItem(stem, id);
        var target = Path.Combine(RunFolder(stem), id);

        if (!Directory.Exists(source)) return Result.Failed<Unit>("not in the bin: " + id);
        if (Directory.Exists(target)) return Result.Failed<Unit>("a backup " + id + " is already in the list");

        try
        {
            Directory.CreateDirectory(RunFolder(stem));
            Directory.Move(source, target);
            TryDeleteFile(Path.Combine(target, BinnedFile));
            TryDeleteEmpty(Path.Combine(BinPath, stem));

            return Result.Done(new Unit());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<Unit>(ex.Message);
        }
    }

    public Result<Unit> DeleteFromBin(string stem, string id)
    {
        if (!IsSafeId(stem) || !IsSafeId(id)) return Result.Failed<Unit>("not a backup: " + stem + "/" + id);

        if (!TryDeleteFolder(BinItem(stem, id))) return Result.Failed<Unit>("could not delete " + id);

        TryDeleteEmpty(Path.Combine(BinPath, stem));

        return Result.Done(new Unit());
    }

    /// <summary>Deletes every backup in the bin and every deleted save's backups, pinned ones too; returns how many went.</summary>
    public int EmptyBin()
    {
        var removed = Bin().Count(entry => DeleteFromBin(entry.Meta.Stem, entry.Meta.Id) is Result<Unit>.Done);

        foreach (var stem in DeletedRuns().Keys.ToList()) removed += DeleteRunForever(stem, isPinnedToo: true);

        return removed;
    }

    /// <summary>Runs the game no longer lists, with when the mod saw them go.</summary>
    public IReadOnlyDictionary<string, DateTime> DeletedRuns() =>
        AllNotes().Where(pair => pair.Value.DeletedAt != null).ToDictionary(pair => pair.Key, pair => pair.Value.DeletedAt!.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Notes that the run left the game's list, once: a later call keeps the first time.</summary>
    public Result<Unit> MarkDeleted(string stem, DateTime utc) =>
        Directory.Exists(RunFolder(stem)) ? UpdateNote(stem, note => note.DeletedAt != null ? note : note with { DeletedAt = utc }) : Result.Done(new Unit());

    /// <summary>The run is back in the game's list.</summary>
    public Result<Unit> MarkListed(string stem) =>
        ReadNote(stem).DeletedAt == null ? Result.Done(new Unit()) : UpdateNote(stem, note => note with { DeletedAt = null });

    /// <summary>
    /// Deletes a deleted save's backups for good, and its folder when none are left. Pinned backups stay unless
    /// <paramref name="isPinnedToo"/>. Returns how many went.
    /// </summary>
    public int DeleteRunForever(string stem, bool isPinnedToo)
    {
        if (!IsSafeId(stem) || !IsRunFolder(stem)) return 0;

        var removed = Of(stem).Where(meta => isPinnedToo || !meta.IsPinned).Count(meta => DeleteForever(meta.Stem, meta.Id) is Result<Unit>.Done);

        if (Of(stem).Count == 0) TryDeleteFolder(RunFolder(stem));

        return removed;
    }

    /// <summary>Deletes for good what has been in the bin, or deleted from the game's list, longer than <paramref name="days"/>. 0 keeps everything.</summary>
    public int ExpireBin(int days)
    {
        if (days <= 0) return 0;

        var cutoff = _utcNow().AddDays(-days);
        var removed = Bin().Where(entry => entry.BinnedUtc < cutoff).Count(entry => DeleteFromBin(entry.Meta.Stem, entry.Meta.Id) is Result<Unit>.Done);

        foreach (var pair in DeletedRuns().Where(pair => pair.Value < cutoff).ToList()) removed += DeleteRunForever(pair.Key, isPinnedToo: false);

        return removed;
    }

    /// <summary>
    /// Copies every backup not yet in <paramref name="secondRoot"/> there, each through a ".partial" folder, with its
    /// run's run.json. A backup in the second copy that is gone from a run still here is removed there too, so the
    /// copy follows the limits; a run gone from here entirely is left alone there. Returns how many were copied.
    /// </summary>
    public Result<int> Mirror(string secondRoot)
    {
        if (string.IsNullOrWhiteSpace(secondRoot)) return Result.Done(0);

        if (string.Equals(Path.GetFullPath(secondRoot).TrimEnd('/', '\\'), Path.GetFullPath(_root).TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase))
            return Result.Failed<int>("the second folder is the backup folder itself");

        var copied = 0;

        try
        {
            foreach (var group in All().GroupBy(meta => meta.Stem))
            {
                var mirrorRun = Path.Combine(secondRoot, group.Key);
                var ids = group.Select(meta => meta.Id).ToHashSet(StringComparer.Ordinal);

                Directory.CreateDirectory(mirrorRun);

                foreach (var meta in group)
                {
                    var target = Path.Combine(mirrorRun, meta.Id);

                    if (Directory.Exists(target)) continue;

                    var partial = target + PartialSuffix;

                    TryDeleteFolder(partial);
                    Directory.CreateDirectory(partial);

                    foreach (var file in Directory.EnumerateFiles(Path.Combine(RunFolder(meta.Stem), meta.Id)))
                        File.Copy(file, Path.Combine(partial, Path.GetFileName(file)), overwrite: true);

                    Directory.Move(partial, target);
                    copied++;
                }

                // Pin and note changes are rewritten, so the copy's backup.json follows.
                foreach (var meta in group)
                    File.Copy(Path.Combine(RunFolder(meta.Stem), meta.Id, BackupMeta.FileName), Path.Combine(mirrorRun, meta.Id, BackupMeta.FileName), overwrite: true);

                var note = Path.Combine(RunFolder(group.Key), RunNoteFile);

                if (File.Exists(note)) File.Copy(note, Path.Combine(mirrorRun, RunNoteFile), overwrite: true);

                foreach (var old in Directory.EnumerateDirectories(mirrorRun).Where(folder => !ids.Contains(Path.GetFileName(folder))).ToList())
                    TryDeleteFolder(old);
            }

            return Result.Done(copied);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failed<int>(ex.Message);
        }
    }

    private static (DateTime? At, BinReason Why) ReadBinned(string path)
    {
        try
        {
            if (!File.Exists(path)) return (null, BinReason.Deleted);

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            DateTime? at = root.TryGetProperty("at", out var value)
                           && value.ValueKind == JsonValueKind.String
                           && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
                ? time.ToUniversalTime()
                : null;
            var why = root.TryGetProperty("why", out var reason) && reason.ValueKind == JsonValueKind.String && reason.GetString() == "thinned" ? BinReason.Thinned : BinReason.Deleted;

            return (at, why);
        }
        catch (JsonException)
        {
            return (null, BinReason.Deleted);
        }
    }

    private static void TryDeleteEmpty(string folder)
    {
        try
        {
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An empty folder left behind is harmless.
        }
    }
}
