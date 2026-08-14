using Platformer.Core.Presentation;

namespace Platformer.Core.Tests.Presentation;

public sealed class VirtualViewportTests
{
    private static readonly VirtualViewport Viewport = new();

    [Fact]
    public void Default_IsTheGamesVirtualResolution()
    {
        Assert.Equal(320, Viewport.Width);
        Assert.Equal(180, Viewport.Height);
        Assert.Equal(320, VirtualViewport.DefaultWidth);
        Assert.Equal(180, VirtualViewport.DefaultHeight);
    }

    [Theory]
    [InlineData(0, 180)]
    [InlineData(320, 0)]
    [InlineData(-1, -1)]
    public void Constructor_NonPositiveVirtualSize_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VirtualViewport(width, height));
    }

    [Theory]
    [InlineData(320, 180, 1)]
    [InlineData(640, 360, 2)]
    [InlineData(1280, 720, 4)]
    [InlineData(1920, 1080, 6)]
    [InlineData(1000, 700, 3)]
    [InlineData(800, 600, 2)]
    [InlineData(1279, 719, 3)]
    public void LayoutFor_UsesTheLargestWholeScaleThatFits(int windowWidth, int windowHeight, int expected)
    {
        var layout = Viewport.LayoutFor(windowWidth, windowHeight);

        Assert.Equal(expected, layout.Scale);
        Assert.True(layout.Width <= windowWidth && layout.Height <= windowHeight);

        // ...and one step larger genuinely would not fit, so nothing is wasted.
        var bigger = expected + 1;
        Assert.True((320 * bigger) > windowWidth || (180 * bigger) > windowHeight);
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1000, 700)]
    [InlineData(800, 600)]
    [InlineData(333, 999)]
    [InlineData(1921, 1081)]
    public void LayoutFor_NeverStretchesNonUniformly(int windowWidth, int windowHeight)
    {
        var layout = Viewport.LayoutFor(windowWidth, windowHeight);

        // Cross-multiplied so the aspect check is exact rather than a float
        // comparison: width/height must equal 320/180 with no rounding slack.
        Assert.Equal(layout.Width * 180, layout.Height * 320);
        Assert.Equal(320 * layout.Scale, layout.Width);
        Assert.Equal(180 * layout.Scale, layout.Height);
    }

    [Fact]
    public void LayoutFor_ExactMultiple_FillsTheWindowWithNoBars()
    {
        var layout = Viewport.LayoutFor(1280, 720);

        Assert.Equal(new ViewportLayout(4, 0, 0, 1280, 720), layout);
        Assert.Equal(1280, layout.Right);
        Assert.Equal(720, layout.Bottom);
    }

    [Fact]
    public void LayoutFor_TallWindow_LetterboxesTopAndBottom()
    {
        var layout = Viewport.LayoutFor(1280, 900);

        Assert.Equal(4, layout.Scale);
        Assert.Equal(0, layout.X);
        Assert.Equal(90, layout.Y);
        Assert.Equal(900 - layout.Bottom, layout.Y);
    }

    [Fact]
    public void LayoutFor_WideWindow_PillarboxesLeftAndRight()
    {
        var layout = Viewport.LayoutFor(1600, 720);

        Assert.Equal(4, layout.Scale);
        Assert.Equal(160, layout.X);
        Assert.Equal(0, layout.Y);
        Assert.Equal(1600 - layout.Right, layout.X);
    }

    [Theory]
    [InlineData(1281, 720)]
    [InlineData(1280, 721)]
    [InlineData(1333, 787)]
    public void LayoutFor_OddRemainder_KeepsOffsetsWholeAndLosesAtMostOnePixelOfSymmetry(
        int windowWidth,
        int windowHeight)
    {
        var layout = Viewport.LayoutFor(windowWidth, windowHeight);

        var rightBar = windowWidth - layout.Right;
        var bottomBar = windowHeight - layout.Bottom;

        Assert.True(rightBar - layout.X is 0 or 1, $"horizontal bars differ by {rightBar - layout.X}");
        Assert.True(bottomBar - layout.Y is 0 or 1, $"vertical bars differ by {bottomBar - layout.Y}");
    }

    [Theory]
    [InlineData(200, 100)]
    [InlineData(319, 179)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [InlineData(-100, -100)]
    public void LayoutFor_WindowTooSmall_FloorsAtOneRatherThanScalingFractionally(
        int windowWidth,
        int windowHeight)
    {
        var layout = Viewport.LayoutFor(windowWidth, windowHeight);

        // Scaling below 1 is the fractional case this type exists to avoid, and
        // a minimised window reports zero. Neither may produce a broken frame.
        Assert.Equal(1, layout.Scale);
        Assert.Equal(320, layout.Width);
        Assert.Equal(180, layout.Height);
    }

    [Fact]
    public void LayoutFor_WindowTooSmall_CentresTheOverflowInsteadOfCroppingOneSide()
    {
        var layout = Viewport.LayoutFor(200, 100);

        Assert.Equal(-60, layout.X);
        Assert.Equal(-40, layout.Y);
        Assert.Equal(200 - layout.Right, layout.X);
        Assert.Equal(100 - layout.Bottom, layout.Y);
    }

    [Fact]
    public void LayoutFor_ResizingIsPureAndRepeatable()
    {
        // Resizing must be a function of the window size alone: the same size
        // always gives the same layout, and shrinking then growing back returns
        // exactly where it started rather than drifting.
        var original = Viewport.LayoutFor(1280, 720);

        Viewport.LayoutFor(640, 360);
        Viewport.LayoutFor(1917, 1033);
        Viewport.LayoutFor(0, 0);

        Assert.Equal(original, Viewport.LayoutFor(1280, 720));
    }

    [Fact]
    public void LayoutFor_GrowingTheWindowNeverShrinksTheImage()
    {
        var previous = Viewport.LayoutFor(320, 180);

        for (var size = 320; size <= 2000; size += 7)
        {
            var layout = Viewport.LayoutFor(size, size);

            Assert.True(layout.Scale >= previous.Scale, $"scale fell at {size}px");
            previous = layout;
        }
    }

    [Fact]
    public void LayoutFor_ACustomVirtualResolutionIsHonoured()
    {
        var wide = new VirtualViewport(640, 360);

        var layout = wide.LayoutFor(1280, 720);

        Assert.Equal(2, layout.Scale);
        Assert.Equal(1280, layout.Width);
        Assert.Equal(720, layout.Height);
    }
}
