namespace ComputerArchitecture.Mano;

/// <summary>Single-threaded, instruction-boundary Mano Basic Computer interpreter.</summary>
public sealed class Machine
{
    public const int MemorySize = 4096;
    private readonly ushort[] memory = new ushort[MemorySize];
    private readonly Queue<byte> input = new();
    private readonly List<byte> output = [];
    private int pc;
    private bool outputPending;

    public ushort AC { get; set; }
    public bool E { get; set; }
    public int PC { get => pc; set => pc = ValidateAddress(value); }
    public int AR { get; private set; }
    public ushort IR { get; private set; }
    public ushort DR { get; private set; }
    public bool I { get; private set; }
    public byte INPR { get; private set; }
    public byte OUTR { get; private set; }
    public bool FGI { get; private set; }
    public bool FGO { get; private set; } = true;
    public bool IEN { get; private set; }
    public bool R { get; private set; }
    public bool Running { get; private set; } = true;
    public bool AutoCompleteOutput { get; set; } = true;
    public long InstructionsExecuted { get; private set; }
    public long InterruptsEntered { get; private set; }
    public IReadOnlyList<byte> Output => output.AsReadOnly();
    public MachineState State => new(AC, E, PC, AR, IR, DR, I, INPR, OUTR, FGI, FGO, IEN, R, Running);

    public ushort this[int address]
    {
        get => memory[ValidateAddress(address)];
        set => memory[ValidateAddress(address)] = value;
    }

    public void Load(AssemblyProgram program, int? entryPoint = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        int entry = ValidateAddress(entryPoint ?? program.EntryPoint);
        program.GetMemoryImage().CopyTo(memory, 0);
        input.Clear();
        output.Clear();
        AC = IR = DR = 0;
        E = I = FGI = IEN = R = outputPending = false;
        AR = 0;
        INPR = OUTR = 0;
        PC = entry;
        FGO = Running = AutoCompleteOutput = true;
        InstructionsExecuted = InterruptsEntered = 0;
    }

    public void QueueInput(IEnumerable<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        foreach (byte value in bytes) input.Enqueue(value);
    }

    /// <summary>Offer one input byte without overwriting an unread character.</summary>
    public bool TryOfferInput(byte value)
    {
        if (FGI) return false;
        INPR = value;
        FGI = true;
        return true;
    }

    public void SetOutputReady(bool ready)
    {
        FGO = ready;
        outputPending = false;
    }

    public StepResult Step()
    {
        if (!Running) return new(StepKind.Halted, PC, null, State);
        DeviceBoundary();
        if (R)
        {
            memory[0] = (ushort)PC;
            AR = 0;
            PC = 1;
            IEN = R = false;
            InterruptsEntered++;
            return new(StepKind.InterruptEntry, 0, null, State);
        }

        // Requests latch during fetch/decode, before ION/IOF changes IEN.
        bool request = IEN && (FGI || FGO);
        int address = PC;
        AR = PC;
        IR = memory[AR];
        PC = Next(PC);
        I = (IR & 0x8000) != 0;
        AR = IR & 0x0FFF;
        int opcode = (IR >> 12) & 7;
        if (opcode != 7)
        {
            if (I) AR = memory[AR] & 0x0FFF;
            ExecuteMemory(opcode);
        }
        else ExecuteFixed(address);
        InstructionsExecuted++;
        R = request;
        return new(StepKind.Instruction, address, IR, State);
    }

    public RunResult Run(int maxSteps = 1_000_000, CancellationToken cancellationToken = default,
        Action<StepResult>? trace = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSteps);
        long instructions = InstructionsExecuted;
        long interrupts = InterruptsEntered;
        int steps = 0;
        while (Running && steps < maxSteps)
        {
            if (cancellationToken.IsCancellationRequested) return Result(StopReason.Cancelled);
            StepResult step = Step();
            steps++;
            trace?.Invoke(step);
        }
        return Result(Running ? StopReason.StepLimit : StopReason.Halted);

        RunResult Result(StopReason reason) => new(reason, steps,
            InstructionsExecuted - instructions, InterruptsEntered - interrupts, State);
    }

    private void DeviceBoundary()
    {
        if (!FGI && input.TryDequeue(out byte value)) TryOfferInput(value);
        if (AutoCompleteOutput && outputPending)
        {
            FGO = true;
            outputPending = false;
        }
    }

    private void ExecuteMemory(int opcode)
    {
        switch (opcode)
        {
            case 0: DR = memory[AR]; AC &= DR; break;
            case 1:
                DR = memory[AR];
                int sum = AC + DR;
                AC = unchecked((ushort)sum);
                E = sum > ushort.MaxValue;
                break;
            case 2: DR = memory[AR]; AC = DR; break;
            case 3: memory[AR] = AC; break;
            case 4: PC = AR; break;
            case 5: memory[AR] = (ushort)PC; AR = Next(AR); PC = AR; break;
            case 6:
                DR = unchecked((ushort)(memory[AR] + 1));
                memory[AR] = DR;
                if (DR == 0) PC = Next(PC);
                break;
        }
    }

    private void ExecuteFixed(int address)
    {
        switch (IR)
        {
            case 0x7800: AC = 0; break;
            case 0x7400: E = false; break;
            case 0x7200: AC = (ushort)~AC; break;
            case 0x7100: E = !E; break;
            case 0x7080:
                bool rightBit = (AC & 1) != 0;
                AC = (ushort)((AC >> 1) | (E ? 0x8000 : 0));
                E = rightBit;
                break;
            case 0x7040:
                bool leftBit = (AC & 0x8000) != 0;
                AC = unchecked((ushort)((AC << 1) | (E ? 1 : 0)));
                E = leftBit;
                break;
            case 0x7020: AC = unchecked((ushort)(AC + 1)); break;
            case 0x7010: if ((AC & 0x8000) == 0) PC = Next(PC); break;
            case 0x7008: if ((AC & 0x8000) != 0) PC = Next(PC); break;
            case 0x7004: if (AC == 0) PC = Next(PC); break;
            case 0x7002: if (!E) PC = Next(PC); break;
            case 0x7001: Running = false; break;
            case 0xF800: AC = (ushort)((AC & 0xFF00) | INPR); FGI = false; break;
            case 0xF400:
                OUTR = (byte)AC;
                output.Add(OUTR);
                FGO = false;
                outputPending = true;
                break;
            case 0xF200: if (FGI) PC = Next(PC); break;
            case 0xF100: if (FGO) PC = Next(PC); break;
            case 0xF080: IEN = true; break;
            case 0xF040: IEN = false; break;
            default: throw new InvalidInstructionException(address, IR);
        }
    }

    private static int Next(int address) => (address + 1) & 0xFFF;
    internal static int ValidateAddress(int address)
    {
        if (address is < 0 or >= MemorySize) throw new ArgumentOutOfRangeException(nameof(address), "Address must be 000-FFF.");
        return address;
    }
}
