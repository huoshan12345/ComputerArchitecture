using Xunit;

namespace ComputerArchitecture.Mano;

public sealed class AssemblerTests
{
    [Theory]
    [InlineData("AND", 0x0123)] [InlineData("ADD", 0x1123)]
    [InlineData("LDA", 0x2123)] [InlineData("STA", 0x3123)]
    [InlineData("BUN", 0x4123)] [InlineData("BSA", 0x5123)] [InlineData("ISZ", 0x6123)]
    public void MemoryEncodings(string mnemonic, int expected)
    {
        var program = new Assembler().Assemble($"ORG 100\n{mnemonic} 123\n{mnemonic} 123 I\nEND");
        Assert.Equal(expected, program.GetWord(0x100));
        Assert.Equal(expected | 0x8000, program.GetWord(0x101));
    }

    [Theory]
    [InlineData("CLA", 0x7800)] [InlineData("CLE", 0x7400)]
    [InlineData("CMA", 0x7200)] [InlineData("CME", 0x7100)]
    [InlineData("CIR", 0x7080)] [InlineData("CIL", 0x7040)]
    [InlineData("INC", 0x7020)] [InlineData("SPA", 0x7010)]
    [InlineData("SNA", 0x7008)] [InlineData("SZA", 0x7004)]
    [InlineData("SZE", 0x7002)] [InlineData("HLT", 0x7001)]
    [InlineData("INP", 0xF800)] [InlineData("OUT", 0xF400)]
    [InlineData("SKI", 0xF200)] [InlineData("SKO", 0xF100)]
    [InlineData("ION", 0xF080)] [InlineData("IOF", 0xF040)]
    public void FixedEncodings(string mnemonic, int expected) =>
        Assert.Equal(expected, new Assembler().Assemble(mnemonic).GetWord(0));

    [Fact]
    public void ForwardReferencesCommentsAndHexLookingLabels()
    {
        var program = new Assembler().Assemble("/ comment\norg 100\nlda a16 / forward label\nhlt\nA16, dec -3\nend\nINVALID");
        Assert.Equal(0x100, program.EntryPoint);
        Assert.Equal(0x102, program.Symbols["a16"]);
        Assert.Equal(0x2102, program.GetWord(0x100));
        Assert.Equal(0xFFFD, program.GetWord(0x102));
        Assert.Equal(3, program.Listing.Count);
        Assert.Equal(3, program.Listing[0].LineNumber);
    }

    [Fact]
    public void EntrySkipsVectorDataAndMultipleOrgSections()
    {
        var program = TestPrograms.Fixture("interrupt-input");
        Assert.Equal(0x100, program.EntryPoint);
        Assert.Equal(0, program.Symbols["ZRO"]);
        Assert.Equal(0x200, program.Symbols["SRV"]);
        Assert.Equal(0x4200, program.GetWord(1));
    }

    [Fact]
    public void PracticeQ6SymbolTableAndEntireListingMatch()
    {
        var program = TestPrograms.Fixture("population-count");
        Assert.Equal(0x107, program.Symbols["GHI"]);
        Assert.Equal(0x10B, program.Symbols["MNO"]);
        Assert.Equal(0x10F, program.Symbols["JKL"]);
        Assert.Equal(0x110, program.Symbols["ABC"]);
        Assert.Equal(0x111, program.Symbols["DEF"]);
        ushort[] expected = [0x7400,0x7800,0x3110,0x2111,0x7004,0x4107,0x410F,
            0x7040,0x7002,0x410B,0x4107,0x7400,0x6110,0x7004,0x4107,0x7001,0,0x62C1];
        Assert.Equal(expected, program.Listing.Select(x => x.Word));
    }

    [Theory]
    [InlineData("ORG 1000", 1)] [InlineData("HEX 10000", 1)]
    [InlineData("DEC 32768", 1)] [InlineData("DEC -32769", 1)]
    [InlineData("DEC 1.5", 1)] [InlineData("HEX -1", 1)]
    [InlineData("LDA UNKNOWN", 1)] [InlineData("LDA 1000", 1)]
    [InlineData("ADD 10 X", 1)] [InlineData("LDA", 1)]
    [InlineData("CLA 0", 1)] [InlineData("LONG, HLT", 1)]
    [InlineData("X, HEX 0\nX, HEX 1", 2)]
    [InlineData("ORG 100\nHLT\nORG 100\nHEX 0", 4)]
    [InlineData("ORG FFF\nHLT\nHEX 0", 3)]
    [InlineData("END", 1)] [InlineData("ORG 0x100", 1)]
    public void InvalidSourceHasLineDiagnostics(string source, int line)
    {
        var error = Assert.Throws<AssemblyException>(() => new Assembler().Assemble(source));
        Assert.Equal(line, error.LineNumber);
        Assert.Contains($"Line {line}:", error.Message);
    }

    [Fact]
    public void BoundariesDefaultOriginAndDefensiveImageCopy()
    {
        var program = new Assembler().Assemble("DEC -32768\nDEC 32767\nORG FFF\nHEX FFFF");
        Assert.Equal(0, program.EntryPoint);
        Assert.Equal(0x8000, program.GetWord(0));
        Assert.Equal(0x7FFF, program.GetWord(1));
        Assert.Equal(0xFFFF, program.GetWord(0xFFF));
        var image = program.GetMemoryImage();
        image[0] = 0;
        Assert.Equal(0x8000, program.GetWord(0));
    }
}
