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
    public void Another_mods_message_is_not_ours()
    {
        Assert.False(PageCommand.IsOurs("slmod|unpackall|ready"));
        Assert.IsType<PageCommand.Unknown>(PageCommand.Parse("slmod|unpackall|ready"));
    }
}
