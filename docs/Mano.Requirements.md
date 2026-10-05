# Mano Basic Computer Interpreter Requirements

## Purpose and scope

Provide a reusable C# interpreter and command-line runner for the course's Mano Basic Computer assembly language. The solution is named ComputerArchitecture so future course tools can be added independently.

The user confirmed full instruction support, including I/O and Program Interrupt. The interpreter operates at instruction boundaries, not at individual clock cycles or circuit level.

## Deliverables

- `ComputerArchitecture.slnx`, targeting .NET 10.
- Production projects under `src`; automated tests and their assembly fixtures under `test`.
- `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`; no versions in project PackageReference elements.
- A two-pass assembler with source-line diagnostics, a symbol table and memory listing.
- A machine with 4096 16-bit words, single-step execution, bounded run, inspectable state and controllable I/O.
- A CLI accepting an `.asm` file, optional entry PC, input bytes/text, tracing and a step limit.
- Markdown documentation, runnable examples and tests based on the slides, Practice Problem Set #1 and the review notes.

## Assembly requirements

Accept the course's comma-separated labels, slash comments, indirect suffix `I`, all 25 instruction mnemonics, and ORG/END/DEC/HEX. Resolve forward references and encode the official hexadecimal instruction values. Diagnose unknown mnemonics/symbols, malformed operands, duplicate labels, overlapping ORG sections and out-of-range addresses/data.

Do not silently implement a different ISA. Immediate addressing, ADC/SUB, expression operands, macros and a built-in stack are outside scope.

## Execution requirements

- Execute all seven Memory reference instructions, twelve Register reference instructions and six I/O instructions.
- Implement unsigned carry-out, 16-bit wraparound, 12-bit address wraparound, skip, BSA linkage and 17-bit circulate exactly as specified.
- Support self-modifying memory; execution fetches assembled machine words, not cached source statements.
- Provide deterministic queued input, captured output and manual device readiness controls.
- Support interrupt entry through memory addresses 000/001, software preservation of AC/E, and ION followed by indirect return.
- Stop on HLT, cancellation or a configured execution limit. A polling loop without input must not hang the host indefinitely.
- Expose executed instruction count separately from interrupt entry count; tracing includes interrupt entry.

## Acceptance tests

1. Exact encodings/symbol table for Practice Q6; population count of 62C1 is 6.
2. Practice Q2/Q3 arithmetic, indirect addressing, E and PC.
3. Practice Q4 exact branch path and final state.
4. Practice Q7 clears 600-6FF, preserves adjacent sentinels and ends with PTR=0700, CTR=0000.
5. Review-note array sum, subtraction/OR, three-way IF and inline parameter linkage.
6. Every instruction and important boundary case, including PC wrap, BSA at FFF, rotate/INC/ISZ behavior and INP preserving AC's upper byte.
7. Input/output polling, interrupt request timing, ISR save/restore and indirect return, and pending requests while interrupts are disabled.
8. Real CLI smoke runs, error exit codes and bounded execution.

## Source priority

Use the course instruction table and lecture PDFs as authoritative sources. Review-note programs are examples, not an independent ISA definition. Assignment #1 concerns performance/power rather than executable Mano programs; use Practice and lecture examples for functional interpreter tests.

See [Mano.Specification.md](Mano.Specification.md) for exact semantics and clearly labeled host-side choices.
