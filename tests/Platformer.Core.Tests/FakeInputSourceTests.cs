using Platformer.Core.Input;
using Platformer.Core.Tests.Harness;

namespace Platformer.Core.Tests;

public sealed class FakeInputSourceTests
{
    [Fact]
    public void New_HoldsNothing()
    {
        var input = new FakeInputSource();

        Assert.Equal(InputCommand.None, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
        Assert.Equal(InputCommand.None, input.PendingHeld);
    }

    [Fact]
    public void Press_BeforeBeginStep_IsNotYetVisibleToTheSimulation()
    {
        var input = new FakeInputSource();

        input.Press(InputCommand.Jump);

        Assert.Equal(InputCommand.Jump, input.PendingHeld);
        Assert.Equal(InputCommand.None, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
    }

    [Fact]
    public void BeginStep_CommandBecomesActive_ReportsPressedOnThatStep()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Jump);

        Assert.Equal(InputCommand.Jump, input.Held);
        Assert.Equal(InputCommand.Jump, input.Pressed);
    }

    [Fact]
    public void BeginStep_CommandStillHeldNextStep_PressedGoesFalse()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Jump);
        input.BeginStep(InputCommand.Jump);

        Assert.Equal(InputCommand.Jump, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
    }

    [Fact]
    public void BeginStep_CommandHeldForManySteps_PressedFiresExactlyOnce()
    {
        var input = new FakeInputSource();
        var pressedSteps = 0;

        input.Press(InputCommand.Jump);
        for (var i = 0; i < 20; i++)
        {
            input.BeginStep();
            if ((input.Pressed & InputCommand.Jump) != InputCommand.None)
            {
                pressedSteps++;
            }
        }

        Assert.Equal(1, pressedSteps);
        Assert.Equal(InputCommand.Jump, input.Held);
    }

    [Fact]
    public void BeginStep_ReleasedThenPressedAgain_ReportsASecondEdge()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Jump);
        input.BeginStep(InputCommand.Jump);
        input.BeginStep(InputCommand.None);
        input.BeginStep(InputCommand.Jump);

        Assert.Equal(InputCommand.Jump, input.Pressed);
    }

    [Fact]
    public void BeginStep_NewCommandWhileAnotherIsHeld_PressedContainsOnlyTheNewOne()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Right);
        input.Press(InputCommand.Jump).BeginStep();

        Assert.Equal(InputCommand.Right | InputCommand.Jump, input.Held);
        Assert.Equal(InputCommand.Jump, input.Pressed);
    }

    [Fact]
    public void BeginStep_ReleasingOneCommand_LeavesTheOtherHeldWithoutAnEdge()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Right | InputCommand.Jump);
        input.Release(InputCommand.Jump).BeginStep();

        Assert.Equal(InputCommand.Right, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
    }

    [Fact]
    public void Release_OnlyClearsTheNamedCommand()
    {
        var input = new FakeInputSource();

        input.Press(InputCommand.Left | InputCommand.Dash).Release(InputCommand.Left).BeginStep();

        Assert.Equal(InputCommand.Dash, input.Held);
    }

    [Fact]
    public void ReleaseAll_ClearsEveryHeldCommand()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Left | InputCommand.Dash | InputCommand.Jump);
        input.ReleaseAll().BeginStep();

        Assert.Equal(InputCommand.None, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
    }

    [Fact]
    public void SetHeld_ReplacesTheWholeMaskRatherThanMerging()
    {
        var input = new FakeInputSource();

        input.BeginStep(InputCommand.Left | InputCommand.Dash);
        input.SetHeld(InputCommand.Right).BeginStep();

        Assert.Equal(InputCommand.Right, input.Held);
        Assert.Equal(InputCommand.Right, input.Pressed);
    }

    [Fact]
    public void Reset_ClearsStateSoTheNextHoldIsAnEdgeAgain()
    {
        var input = new FakeInputSource();
        input.BeginStep(InputCommand.Jump);
        input.BeginStep(InputCommand.Jump);

        input.Reset();

        Assert.Equal(InputCommand.None, input.Held);
        Assert.Equal(InputCommand.None, input.Pressed);
        Assert.Equal(InputCommand.None, input.PendingHeld);

        input.BeginStep(InputCommand.Jump);
        Assert.Equal(InputCommand.Jump, input.Pressed);
    }

    [Fact]
    public void FakeInputSource_SubstitutesForTheRealInputSource()
    {
        // The simulation only ever sees IInputSource, so the fake must be a
        // drop-in replacement for RaylibInputSource without the window.
        Assert.True(typeof(IInputSource).IsAssignableFrom(typeof(FakeInputSource)));
    }
}
