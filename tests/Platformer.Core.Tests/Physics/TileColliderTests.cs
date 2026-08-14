using System.Numerics;
using Platformer.Core.Levels;
using Platformer.Core.Physics;

namespace Platformer.Core.Tests.Physics;

public sealed class TileColliderTests
{
    private const float TileSize = 16f;
    private const float Step = 1f / 60f;

    // The body every movement issue this sprint will use: 12 x 16 world units,
    // slightly narrower than a tile so it fits through single-tile gaps.
    private const float BodyWidth = 12f;
    private const float BodyHeight = 16f;

    // Standard fixture: 10 x 8 tiles, floor across the bottom two rows, so the
    // floor's top face is at world Y 96 and a resting body sits at Y 80.
    private const float FloorTop = 96f;
    private const float RestingY = FloorTop - BodyHeight;

    private static TileGrid FlatFloor()
    {
        var grid = new TileGrid(10, 8, TileSize);
        for (var x = 0; x < grid.Width; x++)
        {
            grid.SetTile(x, 6, TileKind.Solid);
            grid.SetTile(x, 7, TileKind.Solid);
        }

        return grid;
    }

    /// <summary>Flat floor plus a two-tile-tall wall standing on it at column 5.</summary>
    private static TileGrid FloorWithWall()
    {
        var grid = FlatFloor();
        grid.SetTile(5, 4, TileKind.Solid);
        grid.SetTile(5, 5, TileKind.Solid);
        return grid;
    }

    private static Aabb Body(float x, float y) => new(x, y, BodyWidth, BodyHeight);

    [Fact]
    public void Move_InOpenSpace_AppliesTheFullDeltaAndReportsNothing()
    {
        var grid = FlatFloor();
        var velocity = new Vector2(60f, 30f);

        var result = TileCollider.Move(Body(16f, 16f), velocity, Step, grid);

        Assert.Equal(16f + (60f * Step), result.Box.X);
        Assert.Equal(16f + (30f * Step), result.Box.Y);
        Assert.Equal(TileContacts.None, result.Contacts);
        Assert.Equal(velocity, result.Velocity);
    }

    [Fact]
    public void Move_WithNoVelocity_DoesNotMoveAndReportsNothing()
    {
        var grid = FlatFloor();
        var box = Body(16f, RestingY);

        var result = TileCollider.Move(box, Vector2.Zero, Step, grid);

        Assert.Equal(box, result.Box);
        Assert.Equal(TileContacts.None, result.Contacts);
    }

    [Fact]
    public void Move_Falling_LandsExactlyFlushAndZeroesVerticalVelocity()
    {
        var grid = FlatFloor();

        // Fast enough that the unresolved destination would be well inside the floor.
        var result = TileCollider.Move(Body(16f, 60f), new Vector2(0f, 3000f), Step, grid);

        Assert.True(result.IsGrounded);
        Assert.Equal(RestingY, result.Box.Y);
        Assert.Equal(FloorTop, result.Box.Bottom);
        Assert.Equal(0f, result.Velocity.Y);
    }

    [Fact]
    public void Move_LandedBody_IsFlushAndThereforeNotOverlappingTheFloor()
    {
        var grid = FlatFloor();

        var result = TileCollider.Move(Body(16f, 60f), new Vector2(0f, 3000f), Step, grid);

        // The floor tile it is standing on. Flush is deliberately not an overlap.
        var floorTile = new Aabb(16f, FloorTop, TileSize, TileSize);
        Assert.False(result.Box.Overlaps(floorTile));
    }

    [Fact]
    public void Move_RestingUnderGravity_IsBitStableAndGroundedOnEveryStep()
    {
        // The flush-contact requirement: a body at rest must not oscillate
        // between colliding and not. There is no epsilon anywhere in the
        // resolver, so this asserts exact equality rather than a tolerance --
        // an approximate assertion would hide precisely the drift it exists to
        // catch.
        var grid = FlatFloor();
        var box = Body(32f, RestingY);
        var gravityPerStep = new Vector2(0f, 1000f * Step);

        for (var i = 0; i < 600; i++)
        {
            var result = TileCollider.Move(box, gravityPerStep, Step, grid);

            Assert.True(result.IsGrounded, $"lost ground contact on step {i}");
            Assert.Equal(RestingY, result.Box.Y);
            Assert.Equal(box.X, result.Box.X);
            Assert.Equal(0f, result.Velocity.Y);

            box = result.Box;
        }
    }

    [Theory]
    [InlineData(100f)]
    [InlineData(-100f)]
    public void Move_SlidingAlongAFlatFloor_DoesNotCatchOnTileSeams(float speed)
    {
        // The classic failure of naive tile collision. The speed is deliberately
        // not a divisor of the tile size, so the body straddles seams at
        // arbitrary sub-tile offsets rather than stepping neatly over them.
        var grid = FlatFloor();
        var box = Body(speed > 0f ? 8f : 132f, RestingY);
        var velocity = new Vector2(speed, 1000f * Step);
        var expectedStep = speed * Step;

        for (var i = 0; i < 60; i++)
        {
            var result = TileCollider.Move(box, velocity, Step, grid);

            Assert.False(result.HitWall, $"caught on a seam at step {i}, x={box.X}");
            Assert.Equal(speed, result.Velocity.X);
            Assert.Equal(box.X + expectedStep, result.Box.X);
            Assert.True(result.IsGrounded);
            Assert.Equal(RestingY, result.Box.Y);

            box = result.Box;
        }

        // 60 steps at 100 u/s covers 100 world units: more than six tile seams.
        Assert.True(MathF.Abs(box.X - (speed > 0f ? 8f : 132f)) > 3f * TileSize);
    }

    [Fact]
    public void Move_IntoARightWall_StopsFlushAndZeroesHorizontalVelocity()
    {
        var grid = FloorWithWall();

        var result = TileCollider.Move(Body(40f, 70f), new Vector2(3000f, 0f), Step, grid);

        Assert.Equal(TileContacts.WallRight, result.Contacts);
        Assert.Equal(80f - BodyWidth, result.Box.X);
        Assert.Equal(80f, result.Box.Right);
        Assert.Equal(0f, result.Velocity.X);
    }

    [Fact]
    public void Move_IntoALeftWall_StopsFlushAndZeroesHorizontalVelocity()
    {
        var grid = FloorWithWall();

        var result = TileCollider.Move(Body(120f, 70f), new Vector2(-3000f, 0f), Step, grid);

        Assert.Equal(TileContacts.WallLeft, result.Contacts);
        Assert.Equal(96f, result.Box.X);
        Assert.Equal(0f, result.Velocity.X);
    }

    [Fact]
    public void Move_IntoACeiling_StopsFlushAndZeroesUpwardVelocity()
    {
        var grid = new TileGrid(10, 8, TileSize);
        for (var x = 0; x < grid.Width; x++)
        {
            grid.SetTile(x, 1, TileKind.Solid);
        }

        // Ceiling tile row 1 spans world Y 16..32, so its underside is at 32.
        var result = TileCollider.Move(Body(48f, 70f), new Vector2(0f, -3000f), Step, grid);

        Assert.True(result.HitCeiling);
        Assert.Equal(32f, result.Box.Y);
        Assert.Equal(0f, result.Velocity.Y);
    }

    [Fact]
    public void Move_HeldAgainstAWall_StaysPutAndKeepsReportingIt()
    {
        // #4 relies on this: horizontal velocity must be zeroed every step a
        // body is pushed into a wall, so no momentum is banked up and released
        // when it finally turns around.
        var grid = FloorWithWall();
        var box = Body(80f - BodyWidth, 70f);

        for (var i = 0; i < 30; i++)
        {
            var result = TileCollider.Move(box, new Vector2(600f, 0f), Step, grid);

            Assert.Equal(TileContacts.WallRight, result.Contacts);
            Assert.Equal(box.X, result.Box.X);
            Assert.Equal(0f, result.Velocity.X);

            box = result.Box;
        }
    }

    [Fact]
    public void Move_FlushAgainstAWallWithNoMotion_IsNotReportedAsColliding()
    {
        var grid = FloorWithWall();
        var box = Body(80f - BodyWidth, RestingY);

        var result = TileCollider.Move(box, Vector2.Zero, Step, grid);

        Assert.Equal(TileContacts.None, result.Contacts);
        Assert.Equal(box, result.Box);
    }

    [Fact]
    public void Move_AwayFromAFlushWall_IsUnobstructed()
    {
        var grid = FloorWithWall();
        var box = Body(80f - BodyWidth, 70f);

        var result = TileCollider.Move(box, new Vector2(-600f, 0f), Step, grid);

        Assert.False(result.HitWall);
        Assert.Equal(box.X - (600f * Step), result.Box.X);
    }

    /// <summary>40 x 8 grid with one solid column, one tile thick, at column 20.</summary>
    private static TileGrid SingleColumnWall()
    {
        var grid = new TileGrid(40, 8, TileSize);
        for (var y = 0; y < grid.Height; y++)
        {
            grid.SetTile(20, y, TileKind.Solid);
        }

        return grid;
    }

    /// <summary>8 x 60 grid with one solid row, one tile thick, at the given row.</summary>
    private static TileGrid SingleRowFloor(int row)
    {
        var grid = new TileGrid(8, 60, TileSize);
        for (var x = 0; x < grid.Width; x++)
        {
            grid.SetTile(x, row, TileKind.Solid);
        }

        return grid;
    }

    [Theory]
    // Each body starts flush against the face it is about to hit, so the naive
    // destination-only threshold is exactly tile size plus body extent:
    // (16 + 12) / (1/60) = 1680 u/s horizontally. At and above that speed the
    // destination box clears the wall entirely and a destination-only test sees
    // nothing. Sweeping the whole span between old and new position removes the
    // threshold, so every one of these must still stop on the same face.
    [InlineData(600f)]
    [InlineData(1680f)]
    [InlineData(5000f)]
    [InlineData(100000f)]
    [InlineData(1e30f)]
    public void Move_AtAnySpeed_DoesNotTunnelThroughASingleTileWall(float speed)
    {
        var grid = SingleColumnWall();
        var flush = (20f * TileSize) - BodyWidth;

        var result = TileCollider.Move(Body(flush, 32f), new Vector2(speed, 0f), Step, grid);

        Assert.Equal(TileContacts.WallRight, result.Contacts);
        Assert.Equal(flush, result.Box.X);
    }

    [Theory]
    [InlineData(-600f)]
    [InlineData(-1680f)]
    [InlineData(-100000f)]
    [InlineData(-1e30f)]
    public void Move_AtAnyLeftwardSpeed_DoesNotTunnelThroughASingleTileWall(float speed)
    {
        var grid = SingleColumnWall();
        var flush = 21f * TileSize;

        var result = TileCollider.Move(Body(flush, 32f), new Vector2(speed, 0f), Step, grid);

        Assert.Equal(TileContacts.WallLeft, result.Contacts);
        Assert.Equal(flush, result.Box.X);
    }

    [Theory]
    // Vertically the naive threshold is (16 + 16) / (1/60) = 1920 u/s.
    [InlineData(600f)]
    [InlineData(1920f)]
    [InlineData(50000f)]
    [InlineData(1e30f)]
    public void Move_AtAnySpeed_DoesNotTunnelThroughASingleTileFloor(float speed)
    {
        var grid = SingleRowFloor(40);
        var flush = (40f * TileSize) - BodyHeight;

        var result = TileCollider.Move(Body(32f, flush), new Vector2(0f, speed), Step, grid);

        Assert.Equal(TileContacts.Ground, result.Contacts);
        Assert.Equal(flush, result.Box.Y);
    }

    [Theory]
    [InlineData(-1920f)]
    [InlineData(-50000f)]
    [InlineData(-1e30f)]
    public void Move_AtAnyUpwardSpeed_DoesNotTunnelThroughASingleTileCeiling(float speed)
    {
        var grid = SingleRowFloor(10);
        var flush = 11f * TileSize;

        var result = TileCollider.Move(Body(32f, flush), new Vector2(0f, speed), Step, grid);

        Assert.Equal(TileContacts.Ceiling, result.Contacts);
        Assert.Equal(flush, result.Box.Y);
    }

    [Fact]
    public void Move_AtHugeSpeedFromADistance_StopsAtTheNearestWallNotTheFurthest()
    {
        // Proves the scan is nearest-first across the whole swept span: the body
        // crosses eighteen empty columns in one step and must stop at the first
        // solid one rather than sailing past it or catching on a later one.
        var grid = SingleColumnWall();

        var result = TileCollider.Move(Body(16f, 32f), new Vector2(100000f, 0f), Step, grid);

        Assert.Equal(TileContacts.WallRight, result.Contacts);
        Assert.Equal((20f * TileSize) - BodyWidth, result.Box.X);
    }

    [Fact]
    public void Move_RisingWithNothingAbove_IsUnobstructed()
    {
        // The jump path #5 will spend most of its time on: upward motion
        // through open air must apply in full and report no contact.
        var grid = FlatFloor();
        var box = Body(48f, RestingY);

        var result = TileCollider.Move(box, new Vector2(0f, -900f), Step, grid);

        Assert.Equal(TileContacts.None, result.Contacts);
        Assert.Equal(RestingY - (900f * Step), result.Box.Y);
        Assert.Equal(-900f, result.Velocity.Y);
    }

    [Fact]
    public void Move_DiagonallyIntoAnInsideCorner_StopsOnBothAxesWithoutClimbing()
    {
        // The case that combined-axis resolution gets wrong. The body is moving
        // down and right into the corner where a wall meets the floor. Choosing
        // the axis by smallest penetration would pop it on top of the wall;
        // resolving each axis against its own direction of travel cannot.
        var grid = FloorWithWall();

        var result = TileCollider.Move(Body(64f, 76f), new Vector2(1200f, 1200f), Step, grid);

        Assert.Equal(TileContacts.WallRight | TileContacts.Ground, result.Contacts);
        Assert.Equal(80f - BodyWidth, result.Box.X);
        Assert.Equal(RestingY, result.Box.Y);
        Assert.Equal(Vector2.Zero, result.Velocity);

        // Explicitly not standing on top of the wall, whose top face is at 64.
        Assert.NotEqual(64f - BodyHeight, result.Box.Y);
    }

    [Fact]
    public void Move_OutsideTheGrid_IsEmptySoABodyFallsOffTheMap()
    {
        // TileGrid reports out of bounds as empty, so nothing catches a body
        // below the level. Falling off the map is a fall, not a landing.
        var grid = FlatFloor();

        var result = TileCollider.Move(Body(16f, 200f), new Vector2(0f, 600f), Step, grid);

        Assert.Equal(TileContacts.None, result.Contacts);
        Assert.Equal(200f + (600f * Step), result.Box.Y);
    }

    [Fact]
    public void Move_StartingInsideSolidGeometry_WalksOutInsteadOfBeingEjected()
    {
        // Tiles the body already overlaps are never resolved against. A body
        // spawned in a wall moves normally rather than being flung out, and the
        // same rule is the precondition a one-way platform will need later.
        var grid = new TileGrid(10, 8, TileSize);
        grid.SetTile(5, 4, TileKind.Solid);
        var box = Body(84f, 64f);

        var result = TileCollider.Move(box, new Vector2(240f, 0f), Step, grid);

        Assert.False(result.HitWall);
        Assert.Equal(84f + (240f * Step), result.Box.X);
    }

    [Fact]
    public void Move_IsDeterministic_SoTheSimulationIsReproducible()
    {
        static Aabb RunScenario()
        {
            var grid = FloorWithWall();
            var box = Body(8f, 16f);
            var velocity = new Vector2(137f, 0f);

            for (var i = 0; i < 240; i++)
            {
                velocity.Y += 1000f * Step;
                var result = TileCollider.Move(box, velocity, Step, grid);
                box = result.Box;
                velocity = result.Velocity;
            }

            return box;
        }

        Assert.Equal(RunScenario(), RunScenario());
    }

    [Fact]
    public void Move_NullGrid_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => TileCollider.Move(Body(0f, 0f), Vector2.Zero, Step, null!));
    }

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(0f, float.NaN)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(0f, float.NegativeInfinity)]
    public void Move_NonFiniteVelocity_Throws(float velocityX, float velocityY)
    {
        // A non-finite velocity would produce a non-finite position, and from
        // then on every frame is poisoned. Failing here is the only place the
        // cause is still visible.
        var grid = FlatFloor();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TileCollider.Move(Body(16f, 16f), new Vector2(velocityX, velocityY), Step, grid));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-0.5f)]
    public void Move_InvalidStep_Throws(float dt)
    {
        var grid = FlatFloor();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TileCollider.Move(Body(16f, 16f), Vector2.Zero, dt, grid));
    }

    [Theory]
    [InlineData(float.NaN, 0f, 12f, 16f)]
    [InlineData(0f, float.PositiveInfinity, 12f, 16f)]
    [InlineData(0f, 0f, 0f, 16f)]
    [InlineData(0f, 0f, 12f, -1f)]
    [InlineData(0f, 0f, float.NaN, 16f)]
    public void Move_InvalidBox_Throws(float x, float y, float width, float height)
    {
        var grid = FlatFloor();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TileCollider.Move(new Aabb(x, y, width, height), Vector2.Zero, Step, grid));
    }

    [Fact]
    public void CollisionResult_ExposesTheContactsItWasGiven()
    {
        var none = new CollisionResult(Body(0f, 0f), Vector2.Zero, TileContacts.None);
        Assert.False(none.IsGrounded);
        Assert.False(none.HitCeiling);
        Assert.False(none.HitWall);

        var all = new CollisionResult(
            Body(0f, 0f),
            Vector2.Zero,
            TileContacts.Ground | TileContacts.Ceiling | TileContacts.WallLeft | TileContacts.WallRight);
        Assert.True(all.IsGrounded);
        Assert.True(all.HitCeiling);
        Assert.True(all.HitWall);

        Assert.True(new CollisionResult(Body(0f, 0f), Vector2.Zero, TileContacts.WallLeft).HitWall);
    }
}
