using Platformer.Core.Levels;

namespace Platformer.Core.Tests.Levels;

public sealed class TileKindExtensionsTests
{
    [Theory]
    [InlineData(TileKind.Empty, false)]
    [InlineData(TileKind.Solid, true)]
    public void IsSolid_ClassifiesEveryKind(TileKind kind, bool expected)
    {
        Assert.Equal(expected, kind.IsSolid());
    }

    [Fact]
    public void IsSolid_CoversEveryDeclaredKind()
    {
        // Guards the promise on TileKind: a new kind must be classified here,
        // and nowhere in the collision code.
        Assert.Equal(2, Enum.GetValues<TileKind>().Length);
    }
}
