using Platformer.Core.Levels;

namespace Platformer.Core.Tests.Levels;

public sealed class LevelFormatExceptionTests
{
    [Fact]
    public void Default_HasAMessageThatExplainsItself()
    {
        var error = new LevelFormatException();

        Assert.Contains("level", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithMessage_KeepsIt()
    {
        var error = new LevelFormatException("the pit has no bottom");

        Assert.Equal("the pit has no bottom", error.Message);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public void WithInnerException_KeepsBoth()
    {
        var cause = new IOException("disk fell over");

        var error = new LevelFormatException("could not read the level", cause);

        Assert.Equal("could not read the level", error.Message);
        Assert.Same(cause, error.InnerException);
    }

    [Fact]
    public void HandBuilt_CarriesNoPositionUntilTheParserGivesItOne()
    {
        var error = new LevelFormatException("no position here");

        Assert.Equal(0, error.Line);
        Assert.Equal(0, error.Column);
        Assert.Null(error.SourceName);
    }
}
