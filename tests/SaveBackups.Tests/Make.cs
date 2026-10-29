using SaveBackups.Core;

namespace SaveBackups.Tests;

internal static class Make
{
    public static readonly DateTime Start = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    public static RunFacts Run(string stem = "Save_A", int turn = 1, int day = 3, string name = "Alice") =>
        new(stem + ".bytes", name, new Progress(turn, day, 600), 7, 2, false);

    public static BackupMeta Backup(BackupKind kind, int minutes, int turn = 1, int day = 3, string stem = "Save_A", bool pinned = false, long bytes = 100) =>
        new($"id{minutes}-{kind}", stem, kind, Start.AddMinutes(minutes), Run(stem, turn, day), bytes, "1.1.19611", pinned);
}

/// <summary>A throwaway folder with a save folder and a backup root in it.</summary>
internal sealed class TempDirs : IDisposable
{
    public TempDirs()
    {
        Root = Path.Combine(Path.GetTempPath(), "sb-tests-" + Guid.NewGuid().ToString("N"));
        Saves = Path.Combine(Root, "Saves");
        Backups = Path.Combine(Root, "SaveBackups");
        Directory.CreateDirectory(Saves);
    }

    public string Root { get; }
    public string Saves { get; }
    public string Backups { get; }

    public void Write(string name, string text) => File.WriteAllText(Path.Combine(Saves, name), text);

    public string Read(string name) => File.ReadAllText(Path.Combine(Saves, name));

    public bool Has(string name) => File.Exists(Path.Combine(Saves, name));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
