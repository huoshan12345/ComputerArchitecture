using System.Globalization;
using System.Text;
using ComputerArchitecture.Mano;

if (args.Length == 0 || args is ["--help"] or ["-h"])
{
    Console.WriteLine("Usage: mano program.asm [--pc HEX] [--input TEXT | --input-hex HEXBYTES] [--max-steps N] [--trace] [--output-not-ready]");
    return args.Length == 0 ? 1 : 0;
}

try
{
    string path = args[0];
    int? entry = null;
    int maxSteps = 1_000_000;
    bool trace = false;
    bool outputNotReady = false;
    byte[] input = [];
    bool inputSpecified = false;
    for (int i = 1; i < args.Length; i++)
    {
        string option = args[i];
        if (option == "--trace") { trace = true; continue; }
        if (option == "--output-not-ready") { outputNotReady = true; continue; }
        if (++i >= args.Length) throw new ArgumentException($"Missing value for {option}.");
        string value = args[i];
        switch (option)
        {
            case "--pc":
                entry = int.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                if (entry is < 0 or > 0xFFF) throw new ArgumentException("PC must be 000-FFF.");
                break;
            case "--max-steps":
                maxSteps = int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
                if (maxSteps <= 0) throw new ArgumentException("Step limit must be positive.");
                break;
            case "--input":
            case "--input-hex":
                if (inputSpecified) throw new ArgumentException("Specify only one input option.");
                inputSpecified = true;
                if (option == "--input")
                {
                    if (value.Any(c => c > 127)) throw new ArgumentException("Input text must be ASCII; use --input-hex for raw bytes.");
                    input = Encoding.ASCII.GetBytes(value);
                }
                else input = Convert.FromHexString(value);
                break;
            default: throw new ArgumentException($"Unknown option '{option}'.");
        }
    }

    var program = Assembler.Assemble(File.ReadAllText(path));
    var machine = new Machine();
    machine.Load(program, entry);

    if (outputNotReady)
        machine.SetOutputReady(false);

    machine.QueueInput(input);

    using var cancellation = new CancellationTokenSource();
    ConsoleCancelEventHandler handler = (_, e) =>
    {
        e.Cancel = true;
        // ReSharper disable once AccessToDisposedClosure
        cancellation.Cancel();
    };
    Console.CancelKeyPress += handler;
    RunResult result;
    try
    {
        result = machine.Run(maxSteps, trace ? Trace : null, cancellation.Token);
    }
    finally
    {
        Console.CancelKeyPress -= handler;
    }

    Console.WriteLine($"{result.Reason}: steps={result.Steps}, instructions={result.Instructions}, interrupts={result.Interrupts}");
    Console.WriteLine($"PC={machine.PC:X3} AC={machine.AC:X4} E={(machine.E ? 1 : 0)} I={(machine.I ? 1 : 0)} SC=0000 IEN={(machine.IEN ? 1 : 0)} FGI={(machine.FGI ? 1 : 0)} FGO={(machine.FGO ? 1 : 0)}");
    Console.WriteLine($"Output hex: {Convert.ToHexString(machine.Output.ToArray())}");
    Console.WriteLine($"Output text (escaped): {System.Text.Json.JsonSerializer.Serialize(Encoding.Latin1.GetString(machine.Output.ToArray()))}");
    return result.Reason switch { StopReason.Halted => 0, StopReason.StepLimit => 2, _ => 3 };

    static void Trace(StepResult step)
    {
        Console.WriteLine($"{step.Kind,-14} @{step.Address:X3} {(step.Instruction is ushort word ? word.ToString("X4") : "----")} " +
                          $"PC={step.State.PC:X3} " +
                          $"AC={step.State.AC:X4} " +
                          $"E={(step.State.E ? 1 : 0)}");
    }
}
catch (Exception exception) when (exception is AssemblyException or InvalidInstructionException or
    ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
