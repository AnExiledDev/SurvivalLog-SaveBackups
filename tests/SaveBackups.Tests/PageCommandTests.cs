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
