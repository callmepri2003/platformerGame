using System.Numerics;
using Platformer.Core.Input;
using Platformer.Core.Levels;
using Platformer.Core.Movement;
using Platformer.Core.Physics;
using Platformer.Core.Tests.Harness;
using Platformer.Core.Time;

namespace Platformer.Core.Tests.Movement;

public sealed class PlayerBodyTests
{
    private const float Step = FixedStepClock.FixedDelta;

    private static readonly MovementTuning Tuning = MovementTuning.Default;

    /// <summary>Flat ground with the player standing on it.</summary>
    private static Level FlatGround() => AsciiLevelLoader.Parse(
        """
        ....................
        ....................
        .........@..........
        ####################
        """);

    /// <summary>The player starts well above the floor, so it is genuinely airborne.</summary>
    private static Level HighAboveGround() => AsciiLevelLoader.Parse(
        """
        ....................
        .........@..........
        ....................
        ....................
        ....................
        ####################
        """);

    /// <summary>
    /// A body standing still on the ground. The first step is spent landing,
    /// because being grounded is reported by the collider once downward motion
    /// has been stopped, so it is not known until a step has run.
    /// </summary>
    private static PlayerBody Standing()
    {
        var body = new PlayerBody(FlatGround());
        body.Advance(InputCommand.None, Step);
        Assert.True(body.IsGrounded);
        return body;
    }

    private static void Hold(PlayerBody body, InputCommand held, int steps)
    {
        for (var i = 0; i < steps; i++)
        {
            body.Advance(held, Step);
        }
    }

    // ---- acceleration -----------------------------------------------------

    [Fact]
    public void HoldingRight_AcceleratesTowardTopSpeedRatherThanSnappingToIt()
    {
        var body = Standing();

        body.Advance(InputCommand.Right, Step);
        var afterOne = body.Velocity.X;

        body.Advance(InputCommand.Right, Step);
        var afterTwo = body.Velocity.X;

        Assert.Equal(Tuning.GroundAcceleration * Step, afterOne);
        Assert.True(afterTwo > afterOne);
        Assert.True(afterTwo < Tuning.MaxSpeed);
    }

    [Fact]
    public void TimeToTopSpeed_IsTheTargetedTenthOfASecond()
    {
        // The issue asks for roughly 0.1s. MaxSpeed / GroundAcceleration is
        // exactly 0.1s, which at 60Hz is six steps.
        var body = Standing();

        Hold(body, InputCommand.Right, 5);
        Assert.True(body.Velocity.X < Tuning.MaxSpeed);

        body.Advance(InputCommand.Right, Step);
        Assert.Equal(Tuning.MaxSpeed, body.Velocity.X);
        Assert.Equal(0.1f, Tuning.MaxSpeed / Tuning.GroundAcceleration, 5);
    }

    [Fact]
    public void TopSpeed_IsNeverExceededNoMatterHowLongTheDirectionIsHeld()
    {
        var body = Standing();

        for (var i = 0; i < 600; i++)
        {
            body.Advance(InputCommand.Right, Step);
            Assert.True(
                MathF.Abs(body.Velocity.X) <= Tuning.MaxSpeed,
                $"exceeded top speed on step {i}: {body.Velocity.X}");
        }

        Assert.Equal(Tuning.MaxSpeed, body.Velocity.X);
    }

    // ---- friction ---------------------------------------------------------

    [Fact]
    public void ReleasingInput_DeceleratesToAFullStop()
    {
        var body = Standing();
        Hold(body, InputCommand.Right, 6);
        Assert.Equal(Tuning.MaxSpeed, body.Velocity.X);

        var steps = 0;
        while (body.Velocity.X != 0f && steps < 60)
        {
            body.Advance(InputCommand.None, Step);
            steps++;
        }

        Assert.Equal(0f, body.Velocity.X);

        // Stopping is quicker than starting, which is what stops the player
        // feeling like they are skating.
        Assert.True(steps * Step < 0.1f, $"took {steps * Step}s to stop");
        Assert.True(Tuning.GroundFriction > Tuning.GroundAcceleration);
    }

    [Fact]
    public void AtRest_TheBodyDoesNotCreep()
    {
        // Friction must land exactly on zero. Subtracting past it would leave
        // the player drifting a fraction of a unit backwards every step.
        var body = Standing();
        var restingAt = body.Position;

        Hold(body, InputCommand.None, 300);

        Assert.Equal(0f, body.Velocity.X);
        Assert.Equal(restingAt, body.Position);
        Assert.True(body.IsGrounded);
    }

    // ---- opposing input ---------------------------------------------------

    [Fact]
    public void HoldingBothDirections_ProducesNoMovementAndNoJitter()
    {
        var body = Standing();
        var restingAt = body.Position;

        for (var i = 0; i < 120; i++)
        {
            body.Advance(InputCommand.Left | InputCommand.Right, Step);
            Assert.Equal(0f, body.Velocity.X);
            Assert.Equal(restingAt, body.Position);
        }
    }

    [Fact]
    public void HoldingBothDirectionsWhileMoving_DeceleratesLikeNoInputAtAll()
    {
        var opposed = Standing();
        var released = Standing();
        Hold(opposed, InputCommand.Right, 6);
        Hold(released, InputCommand.Right, 6);

        opposed.Advance(InputCommand.Left | InputCommand.Right, Step);
        released.Advance(InputCommand.None, Step);

        Assert.Equal(released.Velocity.X, opposed.Velocity.X);
    }

    // ---- turning ----------------------------------------------------------

    [Fact]
    public void TurnaroundIsStrongerThanNeutralFriction()
    {
        Assert.True(Tuning.GroundTurnAcceleration > Tuning.GroundFriction);
        Assert.True(Tuning.AirTurnAcceleration > Tuning.AirFriction);
    }

    [Fact]
    public void Reversing_IsFasterThanStoppingAndStartingAgain()
    {
        static int StepsToReachFullSpeedLeft(PlayerBody body, bool stopFirst)
        {
            var steps = 0;
            if (stopFirst)
            {
                while (body.Velocity.X != 0f && steps < 600)
                {
                    body.Advance(InputCommand.None, Step);
                    steps++;
                }
            }

            while (body.Velocity.X > -MovementTuning.Default.MaxSpeed && steps < 600)
            {
                body.Advance(InputCommand.Left, Step);
                steps++;
            }

            return steps;
        }

        var reversing = Standing();
        var restarting = Standing();
        Hold(reversing, InputCommand.Right, 6);
        Hold(restarting, InputCommand.Right, 6);

        var reverseSteps = StepsToReachFullSpeedLeft(reversing, stopFirst: false);
        var restartSteps = StepsToReachFullSpeedLeft(restarting, stopFirst: true);

        Assert.True(
            reverseSteps < restartSteps,
            $"reversing took {reverseSteps} steps, stopping and restarting took {restartSteps}");
    }

    // ---- air control ------------------------------------------------------

    [Fact]
    public void AirControl_IsWeakerThanGroundControlAndSeparatelyTunable()
    {
        var grounded = Standing();
        var airborne = new PlayerBody(HighAboveGround());
        Assert.False(airborne.IsGrounded);

        grounded.Advance(InputCommand.Right, Step);
        airborne.Advance(InputCommand.Right, Step);

        Assert.Equal(Tuning.GroundAcceleration * Step, grounded.Velocity.X);
        Assert.Equal(Tuning.AirAcceleration * Step, airborne.Velocity.X);
        Assert.True(airborne.Velocity.X < grounded.Velocity.X);
        Assert.True(Tuning.AirAcceleration < Tuning.GroundAcceleration);
    }

    [Fact]
    public void AirFriction_IsWeakerThanGroundFrictionSoAJumpKeepsItsMomentum()
    {
        var airborne = new PlayerBody(HighAboveGround());
        Hold(airborne, InputCommand.Right, 4);
        var carried = airborne.Velocity.X;

        airborne.Advance(InputCommand.None, Step);

        var lost = carried - airborne.Velocity.X;
        Assert.Equal(Tuning.AirFriction * Step, lost, 4);
        Assert.True(Tuning.AirFriction < Tuning.GroundFriction);
    }

    [Fact]
    public void TurningInTheAir_UsesTheAirTurnRateNotTheGroundOne()
    {
        // Air control is weaker across the board, including changing your mind
        // mid-flight -- it is not a special case that reverts to ground values.
        var airborne = new PlayerBody(HighAboveGround());
        Hold(airborne, InputCommand.Right, 4);
        var carried = airborne.Velocity.X;
        Assert.True(carried > 0f);

        airborne.Advance(InputCommand.Left, Step);

        Assert.Equal(carried - (Tuning.AirTurnAcceleration * Step), airborne.Velocity.X, 4);
        Assert.True(Tuning.AirTurnAcceleration < Tuning.GroundTurnAcceleration);
    }

    [Fact]
    public void TuningIsOverridable_SoTestsAndReTuningDoNotEditTheDefaults()
    {
        var body = new PlayerBody(FlatGround(), Tuning with { MaxSpeed = 40f });
        body.Advance(InputCommand.None, Step);

        Hold(body, InputCommand.Right, 60);

        Assert.Equal(40f, body.Velocity.X);
    }

    // ---- collision integration --------------------------------------------

    [Fact]
    public void RunningIntoAWall_StopsAndDoesNotBankMomentum()
    {
        var level = AsciiLevelLoader.Parse(
            """
            ..........
            .@.......#
            ##########
            """);
        var body = new PlayerBody(level);
        body.Advance(InputCommand.None, Step);

        Hold(body, InputCommand.Right, 120);

        Assert.True((body.Contacts & TileContacts.WallRight) != 0);
        Assert.Equal(0f, body.Velocity.X);

        // Releasing must not then unleash stored speed.
        body.Advance(InputCommand.None, Step);
        Assert.Equal(0f, body.Velocity.X);
    }

    // ---- interpolation snapshot -------------------------------------------

    [Fact]
    public void PreviousPosition_IsWhereTheBodyWasAtTheStartOfTheLastStep()
    {
        var body = Standing();
        Hold(body, InputCommand.Right, 6);

        var before = body.Position;
        body.Advance(InputCommand.Right, Step);

        Assert.Equal(before, body.PreviousPosition);
        Assert.NotEqual(before, body.Position);
    }

    [Fact]
    public void InterpolatedPosition_BlendsBetweenTheLastTwoSteps()
    {
        var body = Standing();
        Hold(body, InputCommand.Right, 6);
        var before = body.Position;
        body.Advance(InputCommand.Right, Step);

        Assert.Equal(before, body.InterpolatedPosition(0f));
        Assert.Equal(body.Position, body.InterpolatedPosition(1f));
        Assert.Equal(
            (before.X + body.Position.X) * 0.5f,
            body.InterpolatedPosition(0.5f).X,
            4);
    }

    [Fact]
    public void Spawning_LeavesNothingToInterpolateAcross()
    {
        var body = new PlayerBody(FlatGround());

        Assert.Equal(body.Position, body.PreviousPosition);
        Assert.Equal(body.Position, body.InterpolatedPosition(0.5f));
    }

    [Fact]
    public void Teleport_ResetsTheSnapshotVelocityAndContacts()
    {
        var body = Standing();
        Hold(body, InputCommand.Right, 6);

        body.Teleport(new Vector2(48f, 16f));

        Assert.Equal(new Vector2(48f, 16f), body.Position);
        Assert.Equal(body.Position, body.PreviousPosition);
        Assert.Equal(Vector2.Zero, body.Velocity);
        Assert.Equal(TileContacts.None, body.Contacts);

        // The smear bug: interpolating at any alpha must stay put.
        Assert.Equal(body.Position, body.InterpolatedPosition(0.5f));
    }

    // ---- death plane ------------------------------------------------------

    [Fact]
    public void DeathPlane_IsRelativeToTheLevelRatherThanAFixedCoordinate()
    {
        var shallow = new PlayerBody(FlatGround());
        var deep = new PlayerBody(HighAboveGround());

        Assert.NotEqual(shallow.DeathPlaneY, deep.DeathPlaneY);
        Assert.Equal(
            shallow.Level.Tiles.WorldHeight + (Tuning.DeathPlaneMarginTiles * 16f),
            shallow.DeathPlaneY);
        Assert.True(shallow.DeathPlaneY > shallow.Level.Tiles.WorldHeight);
    }

    [Fact]
    public void FallingIntoThePit_RespawnsThePlayerReadyToPlayAgain()
    {
        // The real shipped level, walking left off the ledge into the pit.
        var level = AsciiLevelLoader.LoadEmbedded(AsciiLevelLoader.TestLevelName);
        var body = new PlayerBody(level);
        var spawn = body.Position;
        body.Advance(InputCommand.None, Step);

        var fellBelowTheLevel = false;
        var respawned = false;

        for (var i = 0; i < 600 && !respawned; i++)
        {
            body.Advance(InputCommand.Left, Step);

            if (body.Position.Y > level.Tiles.WorldHeight)
            {
                fellBelowTheLevel = true;
            }

            if (fellBelowTheLevel && body.Position == spawn)
            {
                respawned = true;
            }
        }

        Assert.True(fellBelowTheLevel, "the player never left the level through the pit");
        Assert.True(respawned, "the player never came back");

        // Reset, not merely repositioned.
        Assert.Equal(spawn, body.Position);
        Assert.Equal(spawn, body.PreviousPosition);
        Assert.Equal(Vector2.Zero, body.Velocity);
        Assert.Equal(TileContacts.None, body.Contacts);

        // And genuinely playable again: lands on solid ground and can run.
        body.Advance(InputCommand.None, Step);
        Assert.True(body.IsGrounded);

        Hold(body, InputCommand.Right, 6);
        Assert.Equal(Tuning.MaxSpeed, body.Velocity.X);
        Assert.True(body.Position.X > spawn.X);
    }

    [Fact]
    public void RespawnFromAnywhere_ReturnsToTheSpawnPoint()
    {
        var body = Standing();
        Hold(body, InputCommand.Right, 30);
        Assert.NotEqual(body.Level.SpawnTopLeft(body.Width, body.Height), body.Position);

        body.Respawn();

        Assert.Equal(body.Level.SpawnTopLeft(body.Width, body.Height), body.Position);
        Assert.Equal(body.Position, body.PreviousPosition);
    }

    // ---- plumbing ---------------------------------------------------------

    [Fact]
    public void NullLevel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayerBody(null!));
    }

    [Fact]
    public void SpawnPlacesTheBodyFlushOnTheGroundWithNothingOverlapping()
    {
        var body = new PlayerBody(FlatGround());

        Assert.Equal(body.Level.SpawnTopLeft(PlayerBody.DefaultWidth, PlayerBody.DefaultHeight), body.Position);
        Assert.Equal(PlayerBody.DefaultWidth, body.Bounds.Width);
        Assert.Equal(PlayerBody.DefaultHeight, body.Bounds.Height);

        var landed = body.Position;
        body.Advance(InputCommand.None, Step);

        Assert.Equal(landed, body.Position);
        Assert.True(body.IsGrounded);
    }

    [Fact]
    public void TheHarnessCanDriveTheRealSimulation()
    {
        // The adapter IFixedStepSimulation was designed to make unnecessary:
        // PlayerBody takes command flags rather than an input source, so it is
        // wired up in one line rather than by reshaping either side.
        var harness = SimulationHarness.For(
            new PlayerBody(FlatGround()),
            static (body, input, dt) => body.Advance(input.Held, dt));

        harness.Run(InputScript.Create().Idle(1).Hold(InputCommand.Right, 6));

        Assert.Equal(Tuning.MaxSpeed, harness.Simulation.Velocity.X);
        Assert.True(harness.Simulation.IsGrounded);
    }

    [Fact]
    public void TheSameScriptAlwaysProducesTheSameResult()
    {
        static (Vector2 Position, Vector2 Velocity) Run()
        {
            var body = new PlayerBody(FlatGround());
            for (var i = 0; i < 120; i++)
            {
                var held = i switch
                {
                    < 20 => InputCommand.Right,
                    < 30 => InputCommand.None,
                    < 90 => InputCommand.Left,
                    _ => InputCommand.Right,
                };

                body.Advance(held, Step);
            }

            return (body.Position, body.Velocity);
        }

        Assert.Equal(Run(), Run());
    }
}
