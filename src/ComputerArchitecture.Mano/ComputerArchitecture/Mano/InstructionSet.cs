namespace ComputerArchitecture.Mano;

internal static class InstructionSet
{
    internal static readonly IReadOnlyDictionary<string, ushort> Memory =
        new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["AND"] = 0x0000, ["ADD"] = 0x1000, ["LDA"] = 0x2000,
            ["STA"] = 0x3000, ["BUN"] = 0x4000, ["BSA"] = 0x5000, ["ISZ"] = 0x6000
        };

    internal static readonly IReadOnlyDictionary<string, ushort> Fixed =
        new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["CLA"] = 0x7800, ["CLE"] = 0x7400, ["CMA"] = 0x7200,
            ["CME"] = 0x7100, ["CIR"] = 0x7080, ["CIL"] = 0x7040,
            ["INC"] = 0x7020, ["SPA"] = 0x7010, ["SNA"] = 0x7008,
            ["SZA"] = 0x7004, ["SZE"] = 0x7002, ["HLT"] = 0x7001,
            ["INP"] = 0xF800, ["OUT"] = 0xF400, ["SKI"] = 0xF200,
            ["SKO"] = 0xF100, ["ION"] = 0xF080, ["IOF"] = 0xF040
        };
}
