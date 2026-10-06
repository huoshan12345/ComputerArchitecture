using Xunit;

namespace ComputerArchitecture.Mano;

public sealed class ReviewNotesTests
{
    [Theory]
    [InlineData(23, 5, 18)]
    [InlineData(-3, 7, 0xFFF6)]
    public void SubtractionProducesExpectedDifference(int a, int b, int expected)
    {
        const string source = """
                    ORG 100
                    LDA B
                    CMA
                    INC
                    ADD A
                    STA DIF
                    HLT
            A,      DEC 0
            B,      DEC 0
            DIF,    HEX 0
                    END
            """;

        // 7.3: A-B using two's complement. Include a negative result.
        var (program, machine) = Load(source);
        machine[program.Symbols["A"]] = unchecked((ushort)a);
        machine[program.Symbols["B"]] = unchecked((ushort)b);
        Halt(machine);
        Assert.Equal(expected, machine[program.Symbols["DIF"]]);
    }

    [Fact]
    public void BitwiseOrProducesExpectedBits()
    {
        const string source = """
                    ORG 100
                    LDA A
                    CMA
                    STA TMP
                    LDA B
                    CMA
                    AND TMP
                    CMA
                    HLT
            A,      HEX 1234
            B,      HEX A501
            TMP,    HEX 0
                    END
            """;

        // 7.3: Bitwise OR through De Morgan, with independent expected bits.
        var (_, machine) = Load(source);
        Halt(machine);
        Assert.Equal(0xB735, machine.AC);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(0x8000)]
    public void ZeroBranchSelectsCorrectTarget(int value)
    {
        const string source = """
                    ORG 100
                    LDA VAL
                    SZA
                    BUN NZR
                    BUN ZER
            NZR,    LDA ONE
                    HLT
            ZER,    CLA
                    HLT
            VAL,    HEX 0
            ONE,    DEC 1
                    END
            """;

        // 7.4: SZA distinguishes the entire zero word from nonzero values.
        var (program, machine) = Load(source);
        machine[program.Symbols["VAL"]] = (ushort)value;
        var path = new List<int>();
        Halt(machine, path);
        int target = program.Symbols[value == 0 ? "ZER" : "NZR"];
        Assert.Contains(target, path);
        Assert.DoesNotContain(program.Symbols[value == 0 ? "NZR" : "ZER"], path);
        Assert.Equal(value == 0 ? 0 : 1, machine.AC);
    }

    [Theory]
    [InlineData(2, 3, 0xFFFF)]
    [InlineData(3, 3, 0)]
    [InlineData(4, 3, 1)]
    public void ArithmeticIfSelectsCorrectBranch(int a, int b, int expected)
    {
        const string source = """
                    ORG 100
                    LDA B
                    CMA
                    INC
                    ADD A
                    SNA
                    BUN CHK
                    BUN L20
            CHK,    SZA
                    BUN L30
                    BUN L25
            L20,    LDA NEG
                    BUN SAV
            L25,    CLA
                    BUN SAV
            L30,    LDA POS
            SAV,    STA RES
                    HLT
            A,      DEC 0
            B,      DEC 0
            NEG,    DEC -1
            POS,    DEC 1
            RES,    HEX 0
                    END
            """;

        // 7.4: Three-way arithmetic IF, within the signed subtraction range.
        var (program, machine) = Load(source);
        machine[program.Symbols["A"]] = (ushort)a;
        machine[program.Symbols["B"]] = (ushort)b;
        Halt(machine);
        Assert.Equal(expected, machine[program.Symbols["RES"]]);
    }

    [Fact]
    public void ArraySumReadsExactlyOneHundredWords()
    {
        const string source = """
                    ORG 100
                    LDA ADS
                    STA PTR
                    LDA NUM
                    STA CTR
                    CLA
            LOP,    ADD PTR I
                    ISZ PTR
                    ISZ CTR
                    BUN LOP
                    STA SUM
                    HLT
            ADS,    HEX 150
            PTR,    HEX 0
            NUM,    DEC -100
            CTR,    HEX 0
            SUM,    HEX 0
                    END
            """;

        // 7.5: Read exactly 100 words, ending at 1B3 with PTR advanced to 1B4.
        var (program, machine) = Load(source);
        for (int i = 0; i < 100; i++) machine[0x150 + i] = (ushort)(i + 1);
        machine[0x14F] = machine[0x1B4] = 0xA55A;
        Halt(machine);
        Assert.Equal(5050, machine[program.Symbols["SUM"]]);
        Assert.Equal(0x1B4, machine[program.Symbols["PTR"]]);
        Assert.Equal(0, machine[program.Symbols["CTR"]]);
        Assert.Equal(0xA55A, machine[0x14F]);
        Assert.Equal(0xA55A, machine[0x1B4]);
    }

    [Fact]
    public void ClearMemoryWritesExactlyTargetRange()
    {
        const string source = """
                    ORG 100         / Place the program starting at hexadecimal address 100.
                    LDA ADS         / AC <- M[ADS] = 0600; load the start address of the range to clear.
                    STA PTR         / M[PTR] <- AC; initialize the pointer to address 600.
                    LDA NUM         / AC <- M[NUM] = FF00, the two's complement representation of -256.
                    STA CTR         / M[CTR] <- AC; initialize the counter, which reaches zero after 256 increments.
                    CLA             / AC <- 0000; prepare the zero value to store on each iteration.
            LOP,    STA PTR I       / Indirect: EA <- low 12 bits of M[PTR]; M[EA] <- AC = 0000.
                    ISZ PTR         / Increment the pointer to the next 16-bit word; it does not wrap to zero in this range.
                    ISZ CTR         / Increment the counter; if zero, skip the next BUN and exit the loop.
                    BUN LOP         / The counter is nonzero; return to LOP to clear the next word.
                    HLT             / All 256 words have been cleared; halt execution.
            ADS,    HEX 600         / Start address constant; this word stores 0600, not the data being cleared.
            PTR,    HEX 0           / Dedicated pointer word, initialized at runtime by STA PTR.
            NUM,    DEC -256        / Negative iteration-count constant; DEC defines data, not a CPU instruction.
            CTR,    HEX 0           / Dedicated counter word, initialized at runtime by STA CTR.
                    END             / End the assembly source; no machine instruction is emitted.
            """;

        // 7.6: Run the actual annotated notes code, including its ORG and END.
        var (program, machine) = Load(source);
        for (int address = 0x5FF; address <= 0x700; address++) machine[address] = 0xA55A;
        Halt(machine);
        for (int address = 0x600; address <= 0x6FF; address++) Assert.Equal(0, machine[address]);
        Assert.Equal(0xA55A, machine[0x5FF]);
        Assert.Equal(0xA55A, machine[0x700]);
        Assert.Equal(0x700, machine[program.Symbols["PTR"]]);
        Assert.Equal(0, machine[program.Symbols["CTR"]]);
        Assert.Equal(0, machine.AC);
    }

    [Fact]
    public void SubroutineLinkagePreservesReturnAddress()
    {
        const string source = """
                    ORG 100
                    BSA SUB
                    / Resume at this instruction after returning.
                    HLT
            SUB,    HEX 0
                    / Subroutine instructions begin at SUB+1.
                    BUN SUB I
                    END
            """;

        // 8.1: BSA saves the fetched PC and BUN SUB I returns to that location.
        var (program, machine) = Load(source);
        machine.AC = 0x1234;
        machine.E = true;
        var path = new List<int>();
        Halt(machine, path);
        Assert.Equal(new[] { 0x100, 0x103, 0x101 }, path);
        Assert.Equal(0x101, machine[program.Symbols["SUB"]]);
        Assert.Equal(0x102, machine.PC);
        Assert.Equal(0x1234, machine.AC);
        Assert.True(machine.E);
    }

    [Fact]
    public void InlineParameterIsLoadedAndSkippedOnReturn()
    {
        const string source = """
                    ORG 100
                    BSA SUB
                    HEX 3AF6
                    STA RES
                    HLT
            SUB,    HEX 0
                    LDA SUB I
                    ISZ SUB
                    BUN SUB I
            RES,    HEX 0
                    END
            """;

        // 8.2: The return path skips the inline HEX parameter word.
        var (program, machine) = Load(source);
        var path = new List<int>();
        Halt(machine, path);
        Assert.Equal(new[] { 0x100, 0x105, 0x106, 0x107, 0x102, 0x103 }, path);
        Assert.Equal(0x3AF6, machine[program.Symbols["RES"]]);
        Assert.Equal(0x102, machine[program.Symbols["SUB"]]);
    }

    [Fact]
    public void InputPollingWaitsAndPreservesUpperByte()
    {
        const string source = """
                    ORG 100
            INW,    SKI
                    BUN INW
                    INP
                    HLT
                    END
            """;

        // 8.3: No input initially; offer it later and preserve AC's upper byte.
        var (_, machine) = Load(source);
        machine.AC = 0xAB00;
        machine.E = true;
        Assert.Equal(StopReason.StepLimit,
            machine.Run(6, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.True(machine.TryOfferInput(0x71));
        Halt(machine);
        Assert.Equal(0xAB71, machine.AC);
        Assert.False(machine.FGI);
        Assert.True(machine.E);
    }

    [Fact]
    public void OutputPollingWaitsForDeviceReadiness()
    {
        const string source = """
                    ORG 100
                    LDA CHR
            OUW,    SKO
                    BUN OUW
                    OUT
                    HLT
            CHR,    HEX 0041
                    END
            """;

        // 8.3: Output polling waits for readiness, then emits one character.
        var (_, machine) = Load(source);
        machine.AutoCompleteOutput = false;
        machine.SetOutputReady(false);
        Assert.Equal(StopReason.StepLimit,
            machine.Run(6, cancellationToken: TestContext.Current.CancellationToken).Reason);
        Assert.Empty(machine.Output);
        machine.SetOutputReady(true);
        Halt(machine);
        Assert.Equal([0x41], machine.Output);
        Assert.False(machine.FGO);
    }

    [Fact]
    public void InterruptVectorRoutesRequestToServiceRoutine()
    {
        const string source = """
                    ORG 000
            ZRO,    HEX 0
                    BUN SRV
                    ORG 100
                    ION
                    BUN RUN
            RUN,    HLT
                    ORG 200
            SRV,    INP
                    IOF
                    BUN ZRO I
                    END
            """;

        // 8.4: The exact vector snippet routes a real input interrupt to SRV.
        var (_, machine) = Load(source);
        machine.SetOutputReady(false);
        machine.QueueInput([0x71]);
        Halt(machine);
        Assert.Equal(1, machine.InterruptsEntered);
        Assert.Equal(0x102, machine[0]);
        Assert.Equal(0x103, machine.PC);
        Assert.Equal(0x71, machine.AC);
        Assert.False(machine.FGI);
        Assert.False(machine.IEN);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(0x8000, false)]
    [InlineData(0x8000, true)]
    [InlineData(0xFFFF, false)]
    [InlineData(0xFFFF, true)]
    public void ServiceRoutineRestoresAcAndE(int ac, bool e)
    {
        const string source = """
                    ORG 000
            ZRO,    HEX 300
                    ORG 100
                    STA SAC
                    CIR
                    STA SE
                    / Check the flags and service the devices.
                    LDA SE
                    CIL
                    LDA SAC
                    ION
                    BUN ZRO I
            SAC,    HEX 0
            SE,     HEX 0
                    ORG 300
                    HLT
                    END
            """;

        // 8.4: Exercise the save/restore snippet with all E/AC boundary combinations.
        var (program, machine) = Load(source);
        machine.SetOutputReady(false);
        machine.AC = (ushort)ac;
        machine.E = e;
        for (int i = 0; i < 3; i++) machine.Step(); // STA SAC; CIR; STA SE.
        Assert.Equal(ac, machine[program.Symbols["SAC"]]);
        // Emulate work performed by the service routine before restoring state.
        machine.AC = 0xDEAD;
        machine.E = !e;
        Halt(machine);
        Assert.Equal(ac, machine.AC);
        Assert.Equal(e, machine.E);
        Assert.Equal(0x301, machine.PC);
        Assert.True(machine.IEN);
    }

    private static (AssemblyProgram Program, Machine Machine) Load(string source)
    {
        var program = new Assembler().Assemble(source);
        var machine = new Machine();
        machine.Load(program);
        return (program, machine);
    }

    private static void Halt(Machine machine, List<int>? path = null)
    {
        var result = machine.Run(2_000, cancellationToken: TestContext.Current.CancellationToken,
            trace: step => path?.Add(step.Address));
        Assert.Equal(StopReason.Halted, result.Reason);
    }
}
