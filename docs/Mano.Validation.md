# Mano Interpreter Validation

## Environment

Validated on 2026-10-05 using Windows, .NET SDK 10.0.401, net10.0 and xUnit v3. Tests execute the real assembler and interpreter; CLI integration tests launch the actual compiled CLI in separate dotnet processes. No mocked CPU implementation is used.

## Commands

```powershell
dotnet test ComputerArchitecture.slnx --no-restore --verbosity minimal
dotnet build ComputerArchitecture.slnx -c Release --no-restore
```

## Results

- 108 tests passed; 0 failed and 0 skipped.
- Release build succeeded with 0 warnings and 0 errors.
- CLI cases were executed as real child processes, including success, failure and step-limit exit codes.

## Coverage and independent expectations

| Area | Evidence |
| --- | --- |
| Instruction encodings | All 25 table instructions, both addressing modes for seven Memory reference instructions |
| Assembler | Forward references, hex-looking labels, comments, two-pass symbol table, ORG sections, signed DEC, source diagnostics, limits and overlap detection |
| Practice Q2 | ADD C229/E=1, AND C832, ISZ D8F3, LDA D8F2; PC=021 |
| Practice Q3 | Indirect ADD through 320 to 9A0; AC=0A62, E=1, PC=3A1; includes masking a pointer's upper bits |
| Practice Q4 | Every expected executed address, AC and PC, including HLT's incremented PC |
| Practice Q5 | Negative, zero and positive branches via two's complement subtraction, under no-overflow assumptions |
| Practice Q6 | Exact 18-word encoding listing and symbol addresses; popcount checked against .NET BitOperations for 263 representative inputs including 62C1 |
| Practice Q7 | Exactly 256 cleared words, unchanged adjacent sentinels, final PTR=0700/CTR=0000 |
| Review-note programs | Array sum 1..100=5050, final PTR=01B4; inline parameter 3AF6; bitwise OR through De Morgan |
| Machine boundaries | ADD overwrites E, circulate uses old E/AC, SPA includes zero, INC/ISZ preserve E, PC/BSA/ISZ wraparound, self-modifying memory, invalid function encodings |
| I/O | Queued input without overwrite, INP upper-byte preservation, raw 00/FF bytes, polling echo, manual output completion and flags |
| Interrupt | ION timing, vector entry, saved return PC, AC/E software restoration, ION plus indirect return, disabled interrupt still permits polling, output-only request |
| Runtime/CLI | Real successful processes, raw-byte input, trace, invalid options/source, execution limit, runtime cancellation, reset/reload and already halted step |
| Persistent requests | Input-only ISR with unserviced FGO=1 reaches the step limit; FGO=0 configuration completes, as documented |

## Scope and limitations

This validates instruction-boundary behavior, not waveform accuracy, physical I/O timing or electrical circuits. Combined Register reference/I/O function-bit words outside the individual course table are rejected. Input scheduling and automatic output completion are host policies documented separately from the ISA. The interpreter does not add immediate addressing, ADC/SUB or a hardware stack.

Assignment #1 contains performance and power questions rather than Mano ASM, so it is not used as executable ISA evidence.
