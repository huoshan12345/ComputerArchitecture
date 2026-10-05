namespace ComputerArchitecture.Mano.Tests;

internal static class TestPrograms
{
    internal static AssemblyProgram Fixture(string name) => new Assembler().Assemble(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".asm")));

    internal static Machine Load(string source)
    {
        var machine = new Machine();
        machine.Load(new Assembler().Assemble(source));
        return machine;
    }
}
