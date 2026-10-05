using System.Globalization;
using System.Text.RegularExpressions;

namespace ComputerArchitecture.Mano;

/// <summary>Two-pass assembler for the course's Mano symbolic code dialect.</summary>
public sealed class Assembler
{
    private sealed record Line(int Number, string Source, string? Label, string Operation, string[] Operands);
    private sealed record Emission(int Address, Line Line);

    public AssemblyProgram Assemble(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var symbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var emissions = new List<Emission>();
        var occupied = new HashSet<int>();
        int location = 0;
        int? entry = null;
        var sourceLines = source.Replace("\r", "", StringComparison.Ordinal).Split('\n');

        for (int i = 0; i < sourceLines.Length; i++)
        {
            Line? line = Parse(sourceLines[i], i + 1);
            if (line is null) continue;
            if (line.Label is not null)
            {
                if (location >= Machine.MemorySize) throw Error(line, "Label address exceeds FFF.");
                if (!symbols.TryAdd(line.Label, location)) throw Error(line, $"Duplicate label '{line.Label}'.");
            }
            if (line.Operation.Length == 0) continue;
            if (line.Operation == "END")
            {
                RequireOperands(line, 0);
                break;
            }
            if (line.Operation == "ORG")
            {
                RequireOperands(line, 1);
                location = Hex(line.Operands[0], 0xFFF, line);
                continue;
            }
            bool memoryInstruction = InstructionSet.Memory.ContainsKey(line.Operation);
            bool fixedInstruction = InstructionSet.Fixed.ContainsKey(line.Operation);
            if (memoryInstruction)
            {
                if (line.Operands.Length is < 1 or > 2 ||
                    (line.Operands.Length == 2 && !line.Operands[1].Equals("I", StringComparison.OrdinalIgnoreCase)))
                    throw Error(line, "Expected address followed by optional I.");
            }
            else if (fixedInstruction) RequireOperands(line, 0);
            else if (line.Operation is "DEC" or "HEX") RequireOperands(line, 1);
            else throw Error(line, $"Unknown mnemonic '{line.Operation}'.");

            if (location >= Machine.MemorySize) throw Error(line, "Emission exceeds address FFF.");
            if (!occupied.Add(location)) throw Error(line, $"Address {location:X3} overlaps an earlier word.");
            if (memoryInstruction || fixedInstruction) entry ??= location;
            emissions.Add(new Emission(location++, line));
        }
        if (emissions.Count == 0) throw new AssemblyException(1, "Program emits no words.");

        var image = new ushort[Machine.MemorySize];
        var listing = new List<AssemblyWord>();
        foreach (var emission in emissions)
        {
            var line = emission.Line;
            ushort word;
            if (InstructionSet.Memory.TryGetValue(line.Operation, out ushort opcode))
            {
                string operand = line.Operands[0];
                int address = symbols.TryGetValue(operand, out int symbolAddress)
                    ? symbolAddress : Address(operand, line);
                word = (ushort)(opcode | address | (line.Operands.Length == 2 ? 0x8000 : 0));
            }
            else if (InstructionSet.Fixed.TryGetValue(line.Operation, out ushort code)) word = code;
            else if (line.Operation == "HEX") word = (ushort)Hex(line.Operands[0], 0xFFFF, line);
            else
            {
                if (!short.TryParse(line.Operands[0], NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out short value))
                    throw Error(line, "DEC requires a signed decimal integer in -32768..32767.");
                word = unchecked((ushort)value);
            }
            image[emission.Address] = word;
            listing.Add(new AssemblyWord(emission.Address, word, line.Number, line.Source));
        }
        // A conventional interrupt vector is data at 000 and BUN at 001,
        // followed by a disjoint main-program section. Do not start in the ISR vector.
        if (entry == 1 && emissions.Count > 2 && emissions[0].Address == 0 &&
            emissions[0].Line.Operation is "HEX" or "DEC" &&
            emissions[1].Address == 1 && emissions[1].Line.Operation == "BUN" &&
            emissions[2].Address > 1)
        {
            var main = emissions.Skip(2).FirstOrDefault(item =>
                InstructionSet.Memory.ContainsKey(item.Line.Operation) ||
                InstructionSet.Fixed.ContainsKey(item.Line.Operation));
            if (main is not null) entry = main.Address;
        }
        return new AssemblyProgram(image, entry ?? emissions[0].Address, symbols, listing);
    }

    private static Line? Parse(string source, int number)
    {
        string text = source.Split('/', 2)[0].Trim();
        if (text.Length == 0) return null;
        string? label = null;
        int comma = text.IndexOf(',');
        if (comma >= 0)
        {
            label = text[..comma].Trim().ToUpperInvariant();
            if (!Regex.IsMatch(label, "^[A-Z][A-Z0-9]{0,2}$", RegexOptions.CultureInvariant))
                throw new AssemblyException(number, "Label must be 1-3 alphanumeric characters, starting with a letter.");
            text = text[(comma + 1)..].Trim();
        }
        string[] tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return new Line(number, source, label, tokens.Length == 0 ? "" : tokens[0].ToUpperInvariant(), tokens.Skip(1).ToArray());
    }

    private static int Address(string token, Line line)
    {
        if (!Regex.IsMatch(token, "^[0-9A-Fa-f]+$", RegexOptions.CultureInvariant))
            throw Error(line, $"Undefined symbol or invalid address '{token}'.");
        return Hex(token, 0xFFF, line);
    }

    private static int Hex(string token, int maximum, Line line)
    {
        if (!Regex.IsMatch(token, "^[0-9A-Fa-f]+$", RegexOptions.CultureInvariant) ||
            !int.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value) ||
            value < 0 || value > maximum)
            throw Error(line, $"Expected unsigned hexadecimal value in 0..{maximum:X}.");
        return value;
    }

    private static void RequireOperands(Line line, int count)
    {
        if (line.Operands.Length != count) throw Error(line, $"{line.Operation} expects {count} operand(s).");
    }

    private static AssemblyException Error(Line line, string message) => new(line.Number, message);
}
