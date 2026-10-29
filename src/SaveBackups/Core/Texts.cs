namespace SaveBackups.Core;

/// <summary>One piece of on-screen text in the two languages the game ships.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;
}

/// <summary>The words of the mod's toasts. The save manager's own words are in page.js.</summary>
public static class Texts
{
    /// <summary>Who a toast is from, on its first line.</summary>
    public static readonly Phrase Source = new("Save Backups", "存档备份");

    public static readonly Phrase BackedUp = new("Backed up {0}, day {1}.", "已备份「{0}」，第 {1} 天。");

    public static readonly Phrase NotNow = new("The game is busy, try again in a moment.", "游戏正忙，请稍后再试。");

    public static readonly Phrase NoRun = new("There is no run to back up.", "当前没有可备份的存档。");

    public static readonly Phrase Failed = new("Save Backups failed, see BepInEx/save-backups.log.", "存档备份出错，详见 BepInEx/save-backups.log。");

    public static readonly Phrase Restored = new("Restored {0} to day {1}.", "已将「{0}」恢复到第 {1} 天。");

    public static readonly Phrase Deleted = new("Deleted {0}. Its backups are kept under Deleted saves.", "已删除「{0}」。它的备份保留在「已删除的存档」中。");

    public static readonly Phrase BroughtBack = new("{0} is back in your saves.", "「{0}」已回到存档列表。");

    public static readonly Phrase Behind = new(
        "{0} is behind its last backup (day {1}). Steam Cloud may have put back an older copy. Open Load to restore it.",
        "「{0}」比最近的备份（第 {1} 天）更旧，可能是 Steam 云同步换回了旧存档。打开「读取存档」即可恢复。");

    public static string Format(Phrase phrase, bool isChinese, params object[] args) => string.Format(phrase.Pick(isChinese), args);
}
