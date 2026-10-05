namespace ComputerArchitecture.Mano;

public enum StepKind { Instruction, InterruptEntry, Halted }
public enum StopReason { Halted, StepLimit, Cancelled }

public sealed record MachineState(
    ushort AC, bool E, int PC, int AR, ushort IR, ushort DR, bool I,
    byte INPR, byte OUTR, bool FGI, bool FGO, bool IEN, bool R, bool Running)
{
    public int SC => 0;
}

public sealed record StepResult(StepKind Kind, int Address, ushort? Instruction, MachineState State);
public sealed record RunResult(StopReason Reason, int Steps, long Instructions, long Interrupts, MachineState State);

public sealed class InvalidInstructionException(int address, ushort word)
    : Exception($"Unsupported instruction {word:X4} fetched at address {address:X3}.")
{
    public int Address { get; } = address;
    public ushort Word { get; } = word;
}
