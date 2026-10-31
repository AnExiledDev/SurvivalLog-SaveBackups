using System;
using System.Collections.Generic;
using System.Globalization;

namespace SaveBackups.Core;

public enum SettingType
{
    Flag,
    Number,
    Folder,
    Key,
}

/// <summary>One config entry the save manager's settings tab may change, named "Section.Key" as in the cfg file.</summary>
public sealed record SettingKey(string Section, string Key, SettingType Type, int Min = 0, int Max = 0)
{
    public string Id => Section + "." + Key;
}

/// <summary>
/// The settings the page may change, and the check on each value it sends. Anything not listed here is refused, so
/// the page can never write a config entry the mod did not offer.
/// </summary>
public static class SettingKeys
{
    public static readonly IReadOnlyList<SettingKey> All = new[]
    {
        new SettingKey("Backups", "Folder", SettingType.Folder),
        new SettingKey("Backups", "SecondFolder", SettingType.Folder),
        new SettingKey("Backups", "OnMorning", SettingType.Flag),
        new SettingKey("Backups", "OnQuit", SettingType.Flag),
        new SettingKey("Backups", "BeforeRebirth", SettingType.Flag),
        new SettingKey("Backups", "BeforeExpedition", SettingType.Flag),
        new SettingKey("Backups", "BeforeWave", SettingType.Flag),
        new SettingKey("Backups", "AtEnding", SettingType.Flag),
        new SettingKey("Backups", "OnTimer", SettingType.Flag),
        new SettingKey("Backups", "TimerMinutes", SettingType.Number, 5, 240),
        new SettingKey("Backups", "TakePictures", SettingType.Flag),
        new SettingKey("Backups", "Verify", SettingType.Flag),
        new SettingKey("Keep", "Morning", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Quit", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Rebirth", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Manual", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Safety", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Expedition", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Wave", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Ending", SettingType.Number, 0, 999),
        new SettingKey("Keep", "Timer", SettingType.Number, 0, 999),
        new SettingKey("Keep", "KeepAllDays", SettingType.Number, 0, 365),
        new SettingKey("Keep", "DailyDays", SettingType.Number, 0, 365),
        new SettingKey("Keep", "PerLoop", SettingType.Flag),
        new SettingKey("Keep", "MaxTotalMB", SettingType.Number, 0, 1000000),
        new SettingKey("Keep", "BinDays", SettingType.Number, 0, 365),
        new SettingKey("Screen", "ReplaceLoadScreen", SettingType.Flag),
        new SettingKey("Screen", "WarnWhenBehind", SettingType.Flag),
        new SettingKey("Screen", "ShowGameSnapshots", SettingType.Flag),
        new SettingKey("Toasts", "AutoBackupToasts", SettingType.Flag),
        new SettingKey("Controls", "BackupKey", SettingType.Key),
    };

    private static readonly Dictionary<string, SettingKey> ById = Index();

    public static SettingKey? Find(string id) => ById.TryGetValue(id, out var key) ? key : null;

    /// <summary>
    /// The value as the cfg file writes it, or null when it is not a value this setting takes. Numbers are whole and in
    /// range; a folder is empty (the default) or a full path with no characters a path cannot hold; a key is a single
    /// word of letters and digits, which the game side checks against its key names.
    /// </summary>
    public static string? Normalize(SettingKey key, string value)
    {
        value = value.Trim();

        switch (key.Type)
        {
            case SettingType.Flag:
                return value is "true" or "false" ? value : null;
            case SettingType.Number:
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= key.Min && number <= key.Max
                    ? number.ToString(CultureInfo.InvariantCulture)
                    : null;
            case SettingType.Folder:
                return IsFolder(value) ? value : null;
            case SettingType.Key:
                return value.Length is > 0 and <= 30 && IsWord(value) ? value : null;
            default:
                return null;
        }
    }

    /// <summary>A full path on Windows (D:/..., D:\..., \\server\...) or Unix (/...), at most 240 characters.</summary>
    public static bool IsFolder(string value)
    {
        if (value.Length == 0) return true;
        if (value.Length > 240) return false;

        foreach (var c in value)
            if (char.IsControl(c) || c is '"' or '<' or '>' or '|' or '*' or '?') return false;

        var isDrive = value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' && value[2] is '/' or '\\';
        var isShare = value.StartsWith(@"\\", StringComparison.Ordinal);
        var isUnix = value[0] == '/';

        return (isDrive || isShare || isUnix) && (value.Length <= 2 || value.IndexOf(':', 2) < 0);
    }

    private static bool IsWord(string value)
    {
        foreach (var c in value)
            if (!(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')) return false;

        return true;
    }

    private static Dictionary<string, SettingKey> Index()
    {
        var index = new Dictionary<string, SettingKey>(StringComparer.Ordinal);

        foreach (var key in All) index[key.Id] = key;

        return index;
    }
}
