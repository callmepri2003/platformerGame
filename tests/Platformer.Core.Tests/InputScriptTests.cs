using Platformer.Core.Input;
using Platformer.Core.Tests.Harness;

namespace Platformer.Core.Tests;

public sealed class InputScriptTests
{
    [Fact]
    public void Create_ProducesAnEmptyScript()
    {
        var script = InputScript.Create();

        Assert.Equal(0, script.Length);
        Assert.Empty(script.Frames);
        Assert.Equal(InputCommand.None, script.CurrentHeld);
    }

    [Fact]
    public void Hold_EmitsOneFramePerStepAndKeepsHolding()
    {
        var script = InputScript.Create().Hold(InputCommand.Right, 3);

        Assert.Equal(
            new[] { InputCommand.Right, InputCommand.Right, InputCommand.Right },
            script.Frames);
        Assert.Equal(InputCommand.Right, script.CurrentHeld);
    }

    [Fact]
    public void Tap_HoldsForOneStepThenReleases()
    {
        var script = InputScript.Create().Tap(InputCommand.Jump).Wait(2);

        Assert.Equal(
            new[] { InputCommand.Jump, InputCommand.None, InputCommand.None },
            script.Frames);
    }

    [Fact]
    public void Tap_WithLength_HoldsForThatManyStepsThenReleases()
    {
        var script = InputScript.Create().Tap(InputCommand.Jump, 3).Wait(1);

        Assert.Equal(
            new[] { InputCommand.Jump, InputCommand.Jump, InputCommand.Jump, InputCommand.None },
            script.Frames);
    }

    [Fact]
    public void WorkedExample_HoldRightThenTapJump_ProducesTheDocumentedFrames()
    {
        var script = InputScript.Create()
            .Hold(InputCommand.Right, 30)
            .Tap(InputCommand.Jump)
            .Wait(2);

        Assert.Equal(33, script.Length);
        Assert.All(script.Frames.Take(30), frame => Assert.Equal(InputCommand.Right, frame));
        Assert.Equal(InputCommand.Right | InputCommand.Jump, script.Frames[30]);
        Assert.Equal(InputCommand.Right, script.Frames[31]);
        Assert.Equal(InputCommand.Right, script.Frames[32]);
    }

    [Fact]
    public void Press_OnItsOwn_EmitsNoSteps()
    {
        var script = InputScript.Create().Press(InputCommand.Left);

        Assert.Equal(0, script.Length);
        Assert.Equal(InputCommand.Left, script.CurrentHeld);
    }

    [Fact]
    public void Release_LeavesOtherCommandsHeld()
    {
        var script = InputScript.Create()
            .Press(InputCommand.Right)
            .Press(InputCommand.Dash)
            .Release(InputCommand.Dash)
            .Wait(1);

        Assert.Equal(new[] { InputCommand.Right }, script.Frames);
    }

    [Fact]
    public void Idle_ReleasesEverythingAndEmitsEmptySteps()
    {
        var script = InputScript.Create().Hold(InputCommand.Right, 1).Idle(2);

        Assert.Equal(
            new[] { InputCommand.Right, InputCommand.None, InputCommand.None },
            script.Frames);
        Assert.Equal(InputCommand.None, script.CurrentHeld);
    }

    [Fact]
    public void Wait_Zero_EmitsNothing()
    {
        var script = InputScript.Create().Hold(InputCommand.Right, 0);

        Assert.Equal(0, script.Length);
    }

    [Fact]
    public void Wait_NegativeSteps_Throws()
    {
        var script = InputScript.Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => script.Wait(-1));
    }

    [Fact]
    public void Then_AppendsTheOtherScriptsStepsAndAdoptsItsHeldState()
    {
        var runUp = InputScript.Create().Hold(InputCommand.Right, 2);
        var jump = InputScript.Create().Hold(InputCommand.Jump, 1);

        var combined = runUp.Then(jump);

        Assert.Equal(
            new[] { InputCommand.Right, InputCommand.Right, InputCommand.Jump },
            combined.Frames);
        Assert.Equal(InputCommand.Jump, combined.CurrentHeld);
    }

    [Fact]
    public void Then_Null_Throws()
    {
        var script = InputScript.Create();

        Assert.Throws<ArgumentNullException>(() => script.Then(null!));
    }

    [Fact]
    public void Frames_AreOrderedAndCarryNoHiddenState()
    {
        var first = InputScript.Create().Hold(InputCommand.Right, 5).Tap(InputCommand.Jump);
        var second = InputScript.Create().Hold(InputCommand.Right, 5).Tap(InputCommand.Jump);

        Assert.Equal(first.Frames, second.Frames);
    }
}
