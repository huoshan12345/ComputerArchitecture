namespace ComputerArchitecture.Mano;

internal static class TestPrograms
{
    internal static AssemblyProgram Fixture(string name) => Assembler.Assemble(
        File.ReadAllText(Path.Combine(Directories.TestData.FullName, name + ".asm")));

    internal static Machine Load(string source)
    {
        var machine = new Machine();
        machine.Load(Assembler.Assemble(source));
        return machine;
    }
}
