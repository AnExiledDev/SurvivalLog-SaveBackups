using SaveBackups.Core;
using Xunit;

namespace SaveBackups.Tests;

public sealed class PageCommandTests
{
    private static PageCommand Parse(string rest) => PageCommand.Parse(PageCommand.Prefix + rest);

    [Fact]
    public void Each_verb_parses_with_its_arguments()
    {
        Assert.IsType<PageCommand.Ready>(Parse("ready"));
        Assert.IsType<PageCommand.NoScript>(Parse("noscript"));
        Assert.IsType<PageCommand.Close>(Parse("close"));
        Assert.Equal(new PageCommand.Load("Save_A.bytes"), Parse("load|Save_A.bytes"));
        Assert.Equal(new PageCommand.BackUp("Save_A"), Parse("backup|Save_A"));
        Assert.Equal(new PageCommand.Restore("Save_A", "20261001-080000-morning"), Parse("restore|Save_A|20261001-080000-morning"));
        Assert.Equal(new PageCommand.DeleteSave("Save_A"), Parse("delete|Save_A"));
        Assert.Equal(new PageCommand.BringBack("Save_A", "x"), Parse("bringback|Save_A|x"));
        Assert.Equal(new PageCommand.Pin("Save_A", "x", true), Parse("pin|Save_A|x|1"));
        Assert.Equal(new PageCommand.Pin("Save_A", "x", false), Parse("pin|Save_A|x|0"));
        Assert.Equal(new PageCommand.Remove("Save_A", "x"), Parse("remove|Save_A|x"));
        Assert.IsType<PageCommand.OpenFolder>(Parse("folder"));
        Assert.IsType<PageCommand.GameList>(Parse("gamelist"));
        Assert.Equal(new PageCommand.Dismiss("Save_A"), Parse("dismiss|Save_A"));
        Assert.Equal(new PageCommand.CopyAsNew("Save_A", "x"), Parse("copy|Save_A|x"));
        Assert.Equal(new PageCommand.Export("Save_A", "x"), Parse("export|Save_A|x"));
        Assert.IsType<PageCommand.Import>(Parse("import"));
        Assert.Equal(new PageCommand.Typing(true), Parse("typing|1"));
        Assert.Equal(new PageCommand.Typing(false), Parse("typing|0"));
    }

    [Fact]
    public void A_note_may_hold_any_text_once_decoded_but_its_backup_must_still_be_safe()
    {
        // The page percent-encodes the note, so "|" or "/" in it never splits the message or reads as a path.
        Assert.Equal(new PageCommand.Note("Save_A", "x", "a|b ../c"), Parse("note|Save_A|x|" + Uri.EscapeDataString("a|b ../c")));
        Assert.Equal(new PageCommand.Note("Save_A", "x", ""), Parse("note|Save_A|x|"));
        Assert.IsType<PageCommand.Unknown>(Parse("note|../x|y|hi"));
        Assert.IsType<PageCommand.Unknown>(Parse("note|Save_A|x|a|b"));
    }

    [Theory]
    [InlineData("restore|..|x")]
    [InlineData("restore|Save_A|../x")]
    [InlineData("remove|Save_A|a\\b")]
    [InlineData("load|C:")]
    [InlineData("delete|")]
    [InlineData("pin|Save_A|x|2")]
    [InlineData("restore|Save_A")]
    [InlineData("explode")]
    [InlineData("copy|..|x")]
    [InlineData("export|Save_A|../x")]
    [InlineData("typing|2")]
    [InlineData("import|x")]
    public void Bad_or_unsafe_messages_are_unknown(string rest)
    {
        Assert.IsType<PageCommand.Unknown>(Parse(rest));
    }

    [Fact]
    public void The_save_manager_verbs_parse_with_their_arguments()
    {
        Assert.Equal(new PageCommand.OpenFolder("Save_A"), Parse("folder|Save_A"));
        Assert.Equal(new PageCommand.ExportRun("Save_A", true, false), Parse("exportrun|Save_A|1|0"));
        Assert.IsType<PageCommand.ImportList>(Parse("importlist"));
        Assert.Equal(new PageCommand.ImportFile("Save_X.bytes"), Parse("importfile|Save_X.bytes"));
        Assert.Equal(new PageCommand.Rename("Save_A", "Day 9 | best"), Parse("rename|Save_A|" + Uri.EscapeDataString("Day 9 | best")));
        Assert.Equal(new PageCommand.RunPin("Save_A", true), Parse("runpin|Save_A|1"));
        Assert.Equal(new PageCommand.SaveNote("Save_A", "a/b"), Parse("runnote|Save_A|" + Uri.EscapeDataString("a/b")));
        Assert.Equal(new PageCommand.Duplicate("Save_A"), Parse("duplicate|Save_A"));
        Assert.Equal(new PageCommand.Detail("Save_A"), Parse("detail|Save_A"));
        Assert.Equal(new PageCommand.SnapshotAsNew("Save_A", "Save_A.snap2.bytes"), Parse("snapnew|Save_A|Save_A.snap2.bytes"));
        Assert.Equal(new PageCommand.BinPutBack("Save_A", "x"), Parse("binback|Save_A|x"));
        Assert.Equal(new PageCommand.BinDelete("Save_A", "x"), Parse("bindel|Save_A|x"));
        Assert.Equal(new PageCommand.BinDeleteRun("Save_A"), Parse("binrundel|Save_A"));
        Assert.IsType<PageCommand.BinEmpty>(Parse("binempty"));
        Assert.IsType<PageCommand.NewGame>(Parse("newgame"));
        Assert.IsType<PageCommand.BackUpNow>(Parse("backupnow"));
        Assert.IsType<PageCommand.OpenInRun>(Parse("openinrun"));
    }

    [Theory]
    [InlineData("Keep.BinDays", "14", "14")]
    [InlineData("Keep.BinDays", " 7 ", "7")]
    [InlineData("Backups.OnTimer", "true", "true")]
    [InlineData("Backups.Folder", "D:/Backups/SL", "D:/Backups/SL")]
    [InlineData("Backups.SecondFolder", "", "")]
    [InlineData("Controls.BackupKey", "F8", "F8")]
    public void A_setting_the_page_may_change_parses_with_its_value(string id, string value, string stored)
    {
        Assert.Equal(new PageCommand.Set(id, stored), Parse("set|" + id + "|" + Uri.EscapeDataString(value)));
    }

    [Theory]
    [InlineData("Keep.BinDays", "400")]
    [InlineData("Keep.BinDays", "-1")]
    [InlineData("Keep.BinDays", "1.5")]
    [InlineData("Backups.TimerMinutes", "2")]
    [InlineData("Backups.OnTimer", "yes")]
    [InlineData("Backups.Folder", "relative/path")]
    [InlineData("Backups.Folder", "D:/a\"b")]
    [InlineData("Backups.Folder", "D:/a:b")]
    [InlineData("Controls.BackupKey", "F8; rm")]
    [InlineData("General.Secret", "1")]
    public void A_setting_outside_its_range_or_not_offered_is_refused(string id, string value)
    {
        Assert.IsType<PageCommand.Unknown>(Parse("set|" + id + "|" + Uri.EscapeDataString(value)));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/home/me/backups")]
    [InlineData(@"\\server\share")]
    [InlineData(@"C:\Backups")]
    public void Full_paths_count_as_folders(string path) => Assert.True(SettingKeys.IsFolder(path));

    [Theory]
    [InlineData("rename|Save_A|")]
    [InlineData("runpin|Save_A|2")]
    [InlineData("exportrun|Save_A|1")]
    [InlineData("snapnew|Save_A|../Save_B.bytes")]
    [InlineData("binback|..|x")]
    [InlineData("duplicate|C:")]
    public void Bad_save_manager_messages_are_unknown(string rest) => Assert.IsType<PageCommand.Unknown>(Parse(rest));

    [Fact]
    public void Another_mods_message_is_not_ours()
    {
        Assert.False(PageCommand.IsOurs("slmod|unpackall|ready"));
        Assert.IsType<PageCommand.Unknown>(PageCommand.Parse("slmod|unpackall|ready"));
    }
}
