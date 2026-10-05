using Xunit;

namespace ComputerArchitecture.Mano;

public sealed class IoAndRuntimeTests
{
    [Fact]
    public void InputPreservesUpperByteAndQueuesDoNotOverwrite()
    {
        var machine = TestPrograms.Load("SKI\nHLT\nINP\nINP\nHLT");
        machine.AC = 0xAB00;
        machine.E = true;
        machine.QueueInput(new byte[] {0x71,0xFF});
        machine.Step();
        Assert.Equal(2, machine.PC);
        Assert.Equal(0x71, machine.INPR);
        Assert.False(machine.TryOfferInput(0x72));
        machine.Step();
        Assert.Equal(0xAB71, machine.AC);
        Assert.False(machine.FGI);
        machine.Step();
        Assert.Equal(0xABFF, machine.AC);
        Assert.True(machine.E);
    }

    [Fact]
    public void PollingEchoConsumesQueuedBytes()
    {
        var machine = new Machine();
        machine.Load(TestPrograms.Fixture("polling-echo"));
        machine.QueueInput(new byte[] {65,66,255});
        Assert.Equal(StopReason.Halted, machine.Run(100, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(new byte[] {65,66,255}, machine.Output);
    }

    [Fact]
    public void OutputClearsFlagAndManualCompletionControlsSko()
    {
        var machine = TestPrograms.Load("OUT\nSKO\nSKO\nHLT");
        machine.AutoCompleteOutput = false;
        machine.AC = 0xAB71;
        machine.E = true;
        machine.Step();
        Assert.Equal(0x71, machine.OUTR);
        Assert.False(machine.FGO);
        Assert.Equal(new byte[] {0x71}, machine.Output);
        machine.Step();
        Assert.Equal(2, machine.PC);
        machine.SetOutputReady(true);
        machine.Step();
        Assert.Equal(4, machine.PC);
        Assert.True(machine.E);
    }

    [Fact]
    public void InterruptRunsFollowingInstructionBeforeEntryAndPreservesAcE()
    {
        var program = TestPrograms.Fixture("interrupt-input");
        var machine = new Machine();
        machine.Load(program);
        machine.SetOutputReady(false);
        machine.AC = 0xBE00;
        machine.E = true;
        machine.QueueInput(new byte[] {0x71});
        machine.Step(); // ION: old IEN was false, so no request latched yet.
        Assert.True(machine.IEN);
        Assert.False(machine.R);
        Assert.Equal(0x101, machine.PC);
        machine.Step(); // BUN LOP completes before interrupt entry.
        Assert.True(machine.R);
        var entry = machine.Step();
        Assert.Equal(StepKind.InterruptEntry, entry.Kind);
        Assert.Equal(0x102, machine[0]);
        Assert.Equal(1, machine.PC);
        Assert.False(machine.IEN);
        Assert.Equal(0xBE00, machine.AC);
        Assert.True(machine.E);
        for (int i = 0; machine.PC != 0x102 && i < 100; i++) machine.Step();
        // This boundary is immediately after BUN ZRO I.
        Assert.Equal(0x102, machine.PC);
        Assert.Equal(0xBE00, machine.AC);
        Assert.True(machine.E);
        Assert.False(machine.R);
        Assert.Equal(1, machine.InterruptsEntered);
        Assert.Equal(StopReason.Halted, machine.Run(100, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(0x71, machine[program.Symbols["CHR"]] & 0xFF);
        Assert.Equal(1, machine[program.Symbols["FLG"]]);
    }

    [Fact]
    public void DisabledInterruptStillPermitsInputAndPendingFlagPersists()
    {
        var machine = TestPrograms.Load("IOF\nSKI\nHLT\nINP\nHLT");
        machine.QueueInput(new byte[] {0x71});
        machine.Step();
        Assert.True(machine.FGI);
        Assert.False(machine.IEN);
        Assert.Equal(StopReason.Halted, machine.Run(cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(0x71, machine.AC);
        Assert.Equal(0, machine.InterruptsEntered);
    }

    [Fact]
    public void OutputReadyCanRequestInterruptWithoutInput()
    {
        var machine = TestPrograms.Load("ORG 100\nION\nCLA\nHLT");
        machine.Step();
        machine.Step();
        Assert.True(machine.R);
        Assert.Equal(StepKind.InterruptEntry, machine.Step().Kind);
        Assert.Equal(0x102, machine[0]);
    }

    [Fact]
    public void InfiniteLoopAndCancellationHaveDistinctStopReasons()
    {
        var machine = TestPrograms.Load("ORG 100\nLOP, BUN LOP");
        Assert.Equal(StopReason.StepLimit, machine.Run(10, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(10, machine.InstructionsExecuted);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Equal(StopReason.Cancelled, machine.Run(cancellationToken: cancellation.Token).Reason);
        Assert.Equal(10, machine.InstructionsExecuted);
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Run(0, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.PC = 0x1000);
    }

    [Fact]
    public void HaltAndReloadResetEverythingIncludingDevices()
    {
        var program = new Assembler().Assemble("OUT\nHLT");
        var machine = new Machine();
        machine.Load(program);
        machine.AC = 65;
        Assert.Equal(StopReason.Halted, machine.Run(cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(2, machine.PC);
        Assert.Equal(StepKind.Halted, machine.Step().Kind);
        machine.Load(program);
        Assert.Empty(machine.Output);
        Assert.Equal(0, machine.AC);
        Assert.Equal(0, machine.PC);
        Assert.True(machine.FGO);
        Assert.False(machine.IEN);
        Assert.True(machine.Running);
        Assert.Equal(0, machine.InstructionsExecuted);
    }
}
