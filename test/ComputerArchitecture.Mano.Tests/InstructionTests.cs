using Xunit;

namespace ComputerArchitecture.Mano.Tests;

public sealed class InstructionTests
{
    [Theory]
    [InlineData("ADD",0xC229,true,0xD8F2)]
    [InlineData("AND",0xC832,false,0xD8F2)]
    [InlineData("ISZ",0xE937,false,0xD8F3)]
    [InlineData("LDA",0xD8F2,false,0xD8F2)]
    public void PracticeQ2(string mnemonic, int ac, bool e, int operand)
    {
        var machine = TestPrograms.Load($"ORG 020\n{mnemonic} 082\nORG 082\nHEX D8F2");
        machine.AC = 0xE937;
        machine.Step();
        Assert.Equal(ac, machine.AC);
        Assert.Equal(e, machine.E);
        Assert.Equal(0x021, machine.PC);
        Assert.Equal(operand, machine[0x082]);
    }

    [Fact]
    public void PracticeQ3IndirectAddMasksPointer()
    {
        var machine = TestPrograms.Load("ORG 3A0\nHEX 9320\nORG 320\nHEX F9A0\nORG 9A0\nHEX 8B9F");
        machine.AC = 0x7EC3;
        machine.Step();
        Assert.Equal(0x0A62, machine.AC);
        Assert.True(machine.E);
        Assert.True(machine.I);
        Assert.Equal(0x3A1, machine.PC);
        Assert.Equal(0x9A0, machine.AR);
        Assert.Equal(0, machine.State.SC);
    }

    [Fact]
    public void AddOverwritesCarryWithoutCarryIn()
    {
        var machine = TestPrograms.Load("ADD 200");
        machine.AC = 1;
        machine.E = true;
        machine[0x200] = 2;
        machine.Step();
        Assert.Equal(3, machine.AC);
        Assert.False(machine.E);
    }

    [Theory]
    [InlineData("CLA",0xFFFF,true,0,true)]
    [InlineData("CLE",0x1234,true,0x1234,false)]
    [InlineData("CMA",0x1234,true,0xEDCB,true)]
    [InlineData("CME",0x1234,true,0x1234,false)]
    [InlineData("INC",0xFFFF,true,0,true)]
    [InlineData("CIR",0x8001,false,0x4000,true)]
    [InlineData("CIR",0x0002,true,0x8001,false)]
    [InlineData("CIL",0x8000,true,0x0001,true)]
    [InlineData("CIL",0x0001,false,0x0002,false)]
    public void RegisterOperationsUseOldValues(string mnemonic, int before, bool e, int after, bool newE)
    {
        var machine = TestPrograms.Load(mnemonic);
        machine.AC = (ushort)before;
        machine.E = e;
        machine.Step();
        Assert.Equal(after, machine.AC);
        Assert.Equal(newE, machine.E);
    }

    [Theory]
    [InlineData("SPA",0,false,true)] [InlineData("SPA",0x8000,false,false)]
    [InlineData("SPA",1,false,true)] [InlineData("SNA",0x8000,false,true)]
    [InlineData("SNA",0,false,false)] [InlineData("SZA",0,false,true)]
    [InlineData("SZA",1,false,false)] [InlineData("SZE",0,false,true)]
    [InlineData("SZE",0,true,false)]
    public void ConditionalSkips(string instruction, int ac, bool e, bool skip)
    {
        var machine = TestPrograms.Load($"ORG FFF\n{instruction}");
        machine.AC = (ushort)ac;
        machine.E = e;
        machine.Step();
        Assert.Equal(skip ? 1 : 0, machine.PC);
        Assert.Equal(ac, machine.AC);
        Assert.Equal(e, machine.E);
    }

    [Fact]
    public void IndirectStoreBranchAndBsaWrap()
    {
        var machine = TestPrograms.Load("ORG 100\nSTA 200 I\nBUN 200 I");
        machine[0x200] = 0xF345;
        machine.AC = 0xABCD;
        machine.E = true;
        machine.Step();
        Assert.Equal(0xABCD, machine[0x345]);
        Assert.True(machine.E);
        machine.Step();
        Assert.Equal(0x345, machine.PC);

        machine = TestPrograms.Load("ORG 100\nBSA 200 I");
        machine[0x200] = 0xFFFF;
        machine.Step();
        Assert.Equal(0x101, machine[0xFFF]);
        Assert.Equal(0, machine.PC);
        Assert.Equal(0, machine.AR);
    }

    [Fact]
    public void IszMemoryWrapSkipsAndPreservesAcE()
    {
        var machine = TestPrograms.Load("ORG FFF\nISZ 200");
        machine[0x200] = 0xFFFF;
        machine.AC = 0x1234;
        machine.E = true;
        machine.Step();
        Assert.Equal(0, machine[0x200]);
        Assert.Equal(1, machine.PC);
        Assert.Equal(0x1234, machine.AC);
        Assert.True(machine.E);
    }

    [Fact]
    public void MachineFetchesSelfModifiedWords()
    {
        var machine = TestPrograms.Load("ORG 100\nLDA COD\nSTA TAR\nBUN TAR\nTAR, HEX 0000\nCOD, HEX 7001");
        Assert.Equal(StopReason.Halted, machine.Run(cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Equal(0x104, machine.PC);
        Assert.Equal(0x7001, machine[0x103]);
    }

    [Theory]
    [InlineData(0x7000)] [InlineData(0x7C00)] [InlineData(0xF000)] [InlineData(0xFFFF)]
    public void UnsupportedFunctionCombinationsAreDiagnosed(int word)
    {
        var machine = TestPrograms.Load($"ORG 100\nHEX {word:X4}");
        var exception = Assert.Throws<InvalidInstructionException>(() => machine.Step());
        Assert.Equal(0x100, exception.Address);
        Assert.Equal(word, exception.Word);
        Assert.Equal(0x101, machine.PC);
    }
}
