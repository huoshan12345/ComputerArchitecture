# ComputerArchitecture

Course tools for ECE 6500 / ENGI 9861. The first tool is a C# assembler and interpreter for the Mano Basic Computer.

The interpreter runs all 25 course instructions, including I/O and Program Interrupt. It models instruction boundaries and deterministic device events; it does not simulate individual clock cycles or circuits.

## Requirements and specification

These documents were written before implementation:

- [Mano requirements](docs/Mano.Requirements.md)
- [Mano specification](docs/Mano.Specification.md): course sources, instruction semantics, assembly dialect, interrupt timing and host-side policies.
- [Validation](docs/Mano.Validation.md): course-derived cases and verification results.

## Layout

```text
ComputerArchitecture.slnx
Directory.Build.props
Directory.Packages.props
src/
  ComputerArchitecture.Mano/          # Reusable assembler and machine
  ComputerArchitecture.Mano.Cli/      # Console runner
test/
  ComputerArchitecture.Mano.Tests/    # Unit, program and real CLI tests
    Fixtures/                        # Course-derived ASM programs
examples/Mano/                        # Standalone runnable examples
docs/
```

NuGet versions are managed centrally with `ManagePackageVersionsCentrally`. Production projects have no third-party package dependencies. Tests use xUnit v3.

## Build and test

Use .NET SDK 10.0.401 or a compatible later patch in the same feature band.

```powershell
dotnet build ComputerArchitecture.slnx
dotnet test ComputerArchitecture.slnx
```

## Run an ASM file

Run commands from this solution directory:

```powershell
dotnet run --project src/ComputerArchitecture.Mano.Cli -- examples/Mano/array-sum.asm
dotnet run --project src/ComputerArchitecture.Mano.Cli -- examples/Mano/polling-echo.asm --input ABC --trace
dotnet run --project src/ComputerArchitecture.Mano.Cli -- examples/Mano/interrupt-input.asm --input q --pc 100 --output-not-ready
```

The array example sums decimal 1 through 100 and finishes with AC=13BA (decimal 5050). The polling example echoes exactly three bytes. The interrupt example stores the input character in CHR and asks the main loop to stop; it preserves AC/E around the service routine. Run this input-only ISR with `--output-not-ready`: otherwise an unserviced FGO=1 repeatedly requests interrupts and can prevent the main program from progressing. A combined input/output ISR must service both flags; this example intentionally demonstrates only input.

Options:

| Option | Meaning |
| --- | --- |
| `--pc HEX` | Override the initial PC, in hexadecimal |
| `--input TEXT` | Queue ASCII input |
| `--input-hex HEXBYTES` | Queue raw bytes, e.g. `00FF71` |
| `--max-steps N` | Bound execution; default 1,000,000 steps |
| `--trace` | Show every instruction and interrupt-entry boundary |
| `--output-not-ready` | Start with FGO=0, useful for input-only interrupt programs |

Exit codes: 0 = HLT, 1 = input/assembly/execution error, 2 = step limit, 3 = cancellation. Ctrl+C cancels the host run. A polling program with insufficient input stops at the step limit instead of hanging indefinitely.

Use symbolic labels of 1-3 characters, `/` comments and the course's ORG/END/DEC/HEX syntax. ORG and address operands are hexadecimal; DEC is signed decimal. ORG affects storage layout, not a runtime branch. Without an explicit PC, the assembler selects the first mnemonic instruction, skipping a conventional 000/001 interrupt vector. See the specification for exact entry-point policy and supported encodings.

## Use the library

```csharp
using ComputerArchitecture.Mano;

var program = new Assembler().Assemble(File.ReadAllText("program.asm"));
var machine = new Machine();
machine.Load(program);
machine.QueueInput(new byte[] { 0x71 });
var result = machine.Run(maxSteps: 10_000);
Console.WriteLine($"{result.Reason}: AC={machine.AC:X4}, PC={machine.PC:X3}");

// Inspect memory using a label from the assembler's address symbol table.
if (program.Symbols.TryGetValue("RES", out int address))
    Console.WriteLine($"RES={machine[address]:X4}");
```

`Step()` executes one instruction or one interrupt-entry boundary and returns a state snapshot. `State` includes PC/AC/E/AR/IR/DR/I/SC and I/O flags. `AssemblyProgram.Listing` provides addresses, words, source lines and line numbers; `Symbols` resolves labels. `Load` resets state and devices. Use `TryOfferInput`, `SetOutputReady` and `AutoCompleteOutput=false` to test manual device timing between steps. The machine is single-threaded.

## Scope boundaries

This is the course's Basic Computer, not a general assembler: no immediate addressing, ADC/SUB, macros, built-in stack or combined register-function encodings. Recursion and multiword arithmetic must be written using the existing instructions. HLT stops execution; interrupt input is supplied by the host rather than an interactive keyboard device.
