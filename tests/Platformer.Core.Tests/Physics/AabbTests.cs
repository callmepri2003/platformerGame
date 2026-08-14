using Platformer.Core.Physics;

namespace Platformer.Core.Tests.Physics;

public sealed class AabbTests
{
    [Fact]
    public void Edges_DeriveFromMinimumCornerAndSize()
    {
        var box = new Aabb(10f, 20f, 12f, 16f);

        Assert.Equal(10f, box.Left);
        Assert.Equal(22f, box.Right);
        Assert.Equal(20f, box.Top);
        Assert.Equal(36f, box.Bottom);
    }

    [Fact]
    public void Overlaps_GenuineIntersection_IsTrue()
    {
        var box = new Aabb(0f, 0f, 10f, 10f);

        Assert.True(box.Overlaps(new Aabb(9.999f, 0f, 10f, 10f)));
        Assert.True(box.Overlaps(new Aabb(-5f, -5f, 10f, 10f)));
        Assert.True(box.Overlaps(new Aabb(2f, 2f, 1f, 1f)));
    }

    [Theory]
    // Touching on each of the four faces in turn. None of these is an overlap:
    // this is the rule that lets a body rest exactly flush on a surface without
    // being reported as colliding on the next step.
    [InlineData(10f, 0f)]
    [InlineData(-10f, 0f)]
    [InlineData(0f, 10f)]
    [InlineData(0f, -10f)]
    public void Overlaps_TouchingExactly_IsNotAnOverlap(float otherX, float otherY)
    {
        var box = new Aabb(0f, 0f, 10f, 10f);

        Assert.False(box.Overlaps(new Aabb(otherX, otherY, 10f, 10f)));
    }

    [Fact]
    public void Overlaps_Separated_IsFalse()
    {
        var box = new Aabb(0f, 0f, 10f, 10f);

        Assert.False(box.Overlaps(new Aabb(20f, 0f, 10f, 10f)));
        Assert.False(box.Overlaps(new Aabb(0f, 20f, 10f, 10f)));
    }
}
