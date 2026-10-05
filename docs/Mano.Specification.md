# Mano Basic Computer Specification

## Course sources

PDF page numbers are physical PDF pages. Files remain in the course workspace; they are not copied into this repository.

- `Basic Computer Instructions.pdf`, p. 1: instruction mnemonics and hexadecimal encodings.
- `4_Basic Computer Organization and Design Part1.pdf`, pp. 2-11: ISA, addressing and assembly syntax; pp. 16-22: Subroutine linkage, inline parameters, I/O and Program Interrupt.
- `Basic Computer Organization and Design Part1_Notes.pdf`, pp. 3-8 and 16-22: completed examples and ISR register preservation. Pages 21-22 were visually checked.
- `5_Basic Computer Organization and Design Part2.pdf`, pp. 18-19 and corresponding standard Mano instruction-cycle model: fetch/decode/indirect/execute and interrupt sequencing.
- `Midterm #1/Practice Problem Set #1.pdf`, Q2-Q7: independent execution and assembly acceptance cases.

Source location: `C:\Users\lijing\OneDrive\文档\留学\MUN\2026 Fall\ENGI-9861 High-Performance Computer Arch`.

## Machine state

Memory has 4096 words of 16 bits, addressed by words, not bytes. Addresses are 000-FFF hexadecimal. AC and memory arithmetic wrap modulo 65536; PC/address arithmetic wraps modulo 4096.

| State | Width | Meaning |
| --- | --- | --- |
| AC | 16 | Accumulator |
| E | 1 | Carry-out / circulate link |
| PC, AR | 12 | Program Counter / Address Register |
| IR, DR | 16 | Instruction Register / Data Register |
| I | 1 | Instruction bit 15 |
| SC | 4 | Sequence Counter; 0 at completed instruction boundaries |
| INPR, OUTR | 8 | Input / Output Register |
| FGI, FGO | 1 | Input / Output Flag |
| IEN | 1 | Interrupt Enable |
| R | 1 | Latched interrupt request |
| S | 1 | Running state; HLT clears it |

The public state exposes instruction-boundary values. AR/DR represent standard Mano microoperation results where applicable; the interpreter does not expose individual timing signals or gate delays.

## Instruction encoding and addressing

Bits 14-12 are opcode. Opcodes 0-6 are Memory reference instructions: bit 15 is I and bits 11-0 are the address field. Direct EA equals that field. Indirect EA equals the low 12 bits of the memory word at that field.

Opcode 7 with I=0 is Register reference; with I=1 it is I/O. This I is not an indirect-addressing request. Immediate addressing is not supported by this ISA.

Fetch: AR <- PC; IR <- M[AR]; PC <- (PC+1) mod 4096. Decode sets I and AR's address field. Consequently the PC saved by BSA or an interrupt is the next execution address, not the address of the instruction just fetched.

## Memory reference instructions

| Instruction | Direct / Indirect code | Behavior |
| --- | --- | --- |
| AND | 0xxx / 8xxx | DR <- M[EA]; AC <- AC AND DR; E unchanged |
| ADD | 1xxx / 9xxx | DR <- M[EA]; AC <- low 16 bits of AC+DR; E <- carry-out, even when 0; old E is not carry-in |
| LDA | 2xxx / Axxx | DR <- M[EA]; AC <- DR; E unchanged |
| STA | 3xxx / Bxxx | M[EA] <- AC; E unchanged |
| BUN | 4xxx / Cxxx | PC <- EA |
| BSA | 5xxx / Dxxx | M[EA] <- zero-extended current PC; AR <- (EA+1) mod 4096; PC <- AR |
| ISZ | 6xxx / Exxx | DR <- (M[EA]+1) mod 65536; M[EA] <- DR; if DR=0 increment PC once more; AC/E unchanged |

## Register reference instructions

All assignments use old values unless a later microoperation explicitly follows another.

| Instruction | Code | Behavior |
| --- | --- | --- |
| CLA | 7800 | AC <- 0; E unchanged |
| CLE | 7400 | E <- 0 |
| CMA | 7200 | AC <- bitwise complement of AC |
| CME | 7100 | E <- complement of E |
| CIR | 7080 | E <- old AC bit 0; AC <- (old AC >> 1) OR (old E << 15) |
| CIL | 7040 | E <- old AC bit 15; AC <- low 16 bits of (old AC << 1) OR old E |
| INC | 7020 | AC <- (AC+1) mod 65536; E unchanged |
| SPA | 7010 | Skip if AC bit 15 is 0, including AC=0 |
| SNA | 7008 | Skip if AC bit 15 is 1 |
| SZA | 7004 | Skip if AC=0 |
| SZE | 7002 | Skip if E=0 |
| HLT | 7001 | S <- 0; fetched PC remains incremented |

Skip increments the already fetched PC modulo 4096. The supported dialect uses the individual encodings in the course table. Combined function-bit words such as 7C00 and empty function words 7000/F000 are rejected as unsupported encodings rather than assigning unspecified ordering to combined operations.

## I/O instructions

| Instruction | Code | Behavior |
| --- | --- | --- |
| INP | F800 | AC low byte <- INPR; AC upper byte unchanged; FGI <- 0 |
| OUT | F400 | OUTR <- AC low byte; FGO <- 0 |
| SKI | F200 | Skip if FGI=1 |
| SKO | F100 | Skip if FGO=1 |
| ION | F080 | IEN <- 1 |
| IOF | F040 | IEN <- 0 |

E is unchanged by every I/O instruction. SKI/SKO check once; waiting requires a program loop. INP/OUT do not implicitly wait for readiness.

### Host device policy (implementation choice)

Reset has AC/E/flags/IEN/R=0 except FGO=1 (an initially ready output device), and S=1. Each step begins with a device boundary: if FGI=0, one queued input byte is placed into INPR and FGI is set. Additional bytes wait without overwriting unread input. OUT records one byte and clears FGO; automatic output completion sets FGO at the next boundary. Automatic output completion can be disabled; the host can explicitly set output readiness or offer input to a free INPR.

CLI input text is ASCII; non-ASCII characters are rejected. Raw byte input supports all 256 values. Host I/O policy is deterministic and does not claim to model physical device latency.

## Program Interrupt

R is sampled during the instruction's fetch/decode portion from IEN AND (FGI OR FGO), before executing that instruction. An enabled request therefore allows the currently fetched instruction to finish, then performs interrupt entry at the next step boundary. This deliberately avoids taking an interrupt immediately after ION: the following BUN ZRO I can execute before a new request is serviced, matching the standard Mano timing model.

Interrupt entry is a separate step: M[000] <- current PC; PC <- 001; IEN <- 0; R <- 0; SC <- 0. It preserves AC/E. A service routine must preserve any registers it changes; hardware does not save AC/E automatically. Return uses ION then BUN ZRO I, with ZRO defined at address 000. Pending input is not cleared by IOF.

A HLT stops further steps, including interrupt entry. The interpreter is single-threaded: device changes are supplied between steps, not concurrently with machine execution.

## Assembly dialect

- Case-insensitive ASCII labels and mnemonics. Labels contain 1-3 alphanumeric characters and begin with a letter, followed by a comma.
- Slash `/` starts a comment. Blank lines and comment-only lines are ignored. A label-only line is supported as a small convenience.
- Memory instruction syntax: `MNEMONIC address [I]`. Address is a label or an unsigned hexadecimal number 000-FFF; declared labels take precedence over hex-looking identifiers such as A16.
- Register/I/O instructions have no operands.
- ORG uses an unsigned hexadecimal address; it sets LC and emits no word.
- DEC uses a signed decimal integer in -32768..32767; it emits the corresponding 16-bit two's complement word.
- HEX uses an unsigned hexadecimal word 0000-FFFF; it emits one word, including raw machine code if desired.
- END emits no word and ends source processing. END is optional; text after END is ignored. No 0x prefixes, expressions or macros.
- Default LC is 000. Overlapping emitted addresses and emission beyond FFF are errors. ORG may start another disjoint section.
- Two passes produce a memory image, address symbol table and source listing. Errors contain 1-based line numbers. No partial result is returned on assembly failure.

### Entry point (implementation choice)

Default entry is the address of the first mnemonic instruction. A conventional interrupt vector (HEX/DEC at 000 and BUN at 001, followed by a later section) is recognized: select the first mnemonic after the vector instead. This is a runner convenience, not an ISA rule; use an explicit PC for other layouts. If a source has only HEX/DEC, use the first emitted word. An empty program is an error. The host/CLI can override PC explicitly. Unwritten memory starts at 0000, which encodes AND 000, not HLT.

## Runtime API and CLI

Loading an image resets machine/device state and replaces memory. A step either executes one instruction, enters an interrupt, or reports an already halted machine. Run has a positive step limit and cancellation support; interrupt entry counts toward the limit. HLT, limit and cancellation have distinct results. Invalid instruction words throw an execution error identifying the fetched address and word; fetch state is retained for diagnosis.

CLI syntax: `mano program.asm [--pc HEX] [--input TEXT | --input-hex HEXBYTES] [--max-steps N] [--trace] [--output-not-ready]`. Exit 0 on HLT, 1 on assembly/runtime/file errors, 2 on step limit, 3 on cancellation. Print final registers and captured output; trace is opt-in. No GUI, debugger UI, clock simulation or interactive terminal scheduling is required.
