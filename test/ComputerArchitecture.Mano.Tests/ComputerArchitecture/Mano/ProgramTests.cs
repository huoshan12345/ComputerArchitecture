using System.Numerics;
using Xunit;

namespace ComputerArchitecture.Mano;

public sealed class ProgramTests
{
    [Fact]
    public void PracticeQ6PopulationCountAcrossRepresentativeWords()
    {
        var program = TestPrograms.Fixture("population-count");
        int[] values = [0,1,0xFFFF,0x8000,0x62C1,0xAAAA,0x5555];
        foreach (int value in values.Concat(Enumerable.Range(0,256)))
        {
            var machine = new Machine();
            machine.Load(program);
            machine[program.Symbols["DEF"]] = (ushort)value;
            Assert.Equal(StopReason.Halted, machine.Run(500, cancellationToken: TestContext.Current.CancellationToken).Reason);
            Assert.Equal(BitOperations.PopCount((uint)value), machine[program.Symbols["ABC"]]);
            Assert.Equal(0, machine.AC);
            Assert.False(machine.E);
            Assert.Equal(value, machine[program.Symbols["DEF"]]);
        }
    }

    [Fact]
    public void PracticeQ7ClearsExactly256Words()
    {
        var program = TestPrograms.Fixture("clear-memory");
        var machine = new Machine();
        machine.Load(program);
        for (int i = 0x5FF; i <= 0x700; i++) machine[i] = 0xA55A;
        Assert.Equal(StopReason.Halted, machine.Run(2000, cancellationToken: TestContext.Current.CancellationToken).Reason);
        for (int i = 0x600; i <= 0x6FF; i++) Assert.Equal(0, machine[i]);
        Assert.Equal(0xA55A, machine[0x5FF]);
        Assert.Equal(0xA55A, machine[0x700]);
        Assert.Equal(0x700, machine[program.Symbols["PTR"]]);
        Assert.Equal(0, machine[program.Symbols["CTR"]]);
        Assert.Equal(0, machine.AC);
    }

    [Fact]
    public void ArraySumAndFinalPointerMatchWordAddressing()
    {
        var program = TestPrograms.Fixture("array-sum");
        var machine = new Machine();
        machine.Load(program);
        Assert.Equal(StopReason.Halted, machine.Run(1000, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(5050, machine[program.Symbols["SUM"]]);
        Assert.Equal(0x1B4, machine[program.Symbols["PTR"]]);
        Assert.Equal(100, machine[0x1B3]);
        Assert.Equal(0, machine[program.Symbols["CTR"]]);
    }

    [Fact]
    public void InlineParameterSkipsDataOnReturn()
    {
        var program = TestPrograms.Fixture("inline-parameter");
        var machine = new Machine();
        machine.Load(program);
        var path = new List<int>();
        Assert.Equal(StopReason.Halted, machine.Run(cancellationToken: TestContext.Current.CancellationToken, trace: step => path.Add(step.Address)).Reason);
        Assert.Equal(new[] {0x100,0x105,0x106,0x107,0x102,0x103}, path);
        Assert.Equal(0x3AF6, machine[program.Symbols["RES"]]);
        Assert.Equal(0x102, machine[program.Symbols["SUB"]]);
    }

    [Fact]
    public void PracticeQ4TraceMatchesEveryAcAndPc()
    {
        var machine = new Machine();
        machine.Load(TestPrograms.Fixture("branch-trace"));
        var trace = new List<StepResult>();
        machine.Run(cancellationToken: TestContext.Current.CancellationToken, trace: trace.Add);
        Assert.Equal([0xA10,0xA11,0xA12,0xA14,0xA15,0xA13], trace.Select(x => x.Address));
        Assert.Equal(new ushort[] {0,0xC1A5,0xC1A5,0x8104,0x8104,0x8104}, trace.Select(x => x.State.AC));
        Assert.Equal([0xA11,0xA12,0xA14,0xA15,0xA13,0xA14], trace.Select(x => x.State.PC));
    }

    [Theory]
    [InlineData(2,3,0xFFFF)] [InlineData(3,3,0)] [InlineData(4,3,1)]
    public void ArithmeticIfSelectsNegativeZeroPositive(int a, int b, int expected)
    {
        var program = TestPrograms.Fixture("arithmetic-if");
        var machine = new Machine();
        machine.Load(program);
        machine[program.Symbols["A"]] = (ushort)a;
        machine[program.Symbols["B"]] = (ushort)b;
        Assert.Equal(StopReason.Halted, machine.Run(cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(expected, machine[program.Symbols["RES"]]);
    }

    [Fact]
    public void BitwiseOrViaDeMorgan()
    {
        var machine = TestPrograms.Load("LDA A\nCMA\nSTA TMP\nLDA B\nCMA\nAND TMP\nCMA\nHLT\nA, HEX 1234\nB, HEX A501\nTMP, HEX 0");
        machine.Run(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0xB735, machine.AC);
    }
}
