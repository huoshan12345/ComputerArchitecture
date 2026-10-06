using System.Collections.Frozen;

namespace ComputerArchitecture.Mano;

public sealed record AssemblyWord(int Address, ushort Word, int LineNumber, string Source);

/// <summary>An assembled memory image, entry point, symbols and source listing.</summary>
public sealed class AssemblyProgram
{
    private readonly ushort[] _memory;

    internal AssemblyProgram(ushort[] memory, int entryPoint,
        IDictionary<string, int> symbols, IList<AssemblyWord> listing)
    {
        this._memory = (ushort[])memory.Clone();
        EntryPoint = entryPoint;
        Symbols = symbols.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        Listing = Array.AsReadOnly(listing.ToArray());
    }

    public int EntryPoint { get; }
    public IReadOnlyDictionary<string, int> Symbols { get; }
    public IReadOnlyList<AssemblyWord> Listing { get; }
    public ushort GetWord(int address) => _memory[Machine.ValidateAddress(address)];
    public ushort[] GetMemoryImage() => (ushort[])_memory.Clone();
}

public sealed class AssemblyException(int lineNumber, string message)
    : Exception($"Line {lineNumber}: {message}")
{
    public int LineNumber { get; } = lineNumber;
}
