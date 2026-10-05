using System.Diagnostics;
using Xunit;

namespace ComputerArchitecture.Mano;

public sealed class CliTests
{
    [Theory]
    [InlineData("population-count.asm", "", 0, "Halted:")]
    [InlineData("inline-parameter.asm", "", 0, "AC=3AF6")]
    [InlineData("array-sum.asm", "", 0, "AC=13BA")]
    [InlineData("polling-echo.asm", "ABC", 0, "Output hex: 414243")]
    [InlineData("interrupt-input.asm", "q", 0, "Halted:")]
    [InlineData("infinite-loop.asm", "", 2, "StepLimit:")]
    public async Task RunsAssemblyThroughRealCli(string fixture, string input, int code, string expected)
    {
        var arguments = new List<string> { Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture),
            "--max-steps", "2000", "--input", input };
        if (fixture == "interrupt-input.asm") arguments.Add("--output-not-ready");
        var result = await RunCli(arguments.ToArray());
        Assert.Equal(code, result.Code);
        Assert.Contains(expected, result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task UnservicedOutputFlagKeepsRequestingInterrupts()
    {
        var result = await RunCli(Path.Combine(AppContext.BaseDirectory, "Fixtures", "interrupt-input.asm"),
            "--input", "q", "--max-steps", "1000");
        Assert.Equal(2, result.Code);
        Assert.Contains("StepLimit:", result.Output);
    }

    [Fact]
    public async Task MalformedSourceReportsLineAndFailureExitCode()
    {
        var result = await RunCli(Path.Combine(AppContext.BaseDirectory, "Fixtures", "invalid-source.asm"));
        Assert.Equal(1, result.Code);
        Assert.Contains("Line 2:", result.Error);
    }

    [Fact]
    public async Task RawByteInputAndTraceAreAvailable()
    {
        var result = await RunCli(Path.Combine(AppContext.BaseDirectory, "Fixtures", "polling-echo.asm"),
            "--input-hex", "00FF71", "--trace");
        Assert.Equal(0, result.Code);
        Assert.Contains("Output hex: 00FF71", result.Output);
        Assert.Contains("Instruction", result.Output);
    }

    [Theory]
    [InlineData("--pc", "1000")]
    [InlineData("--max-steps", "0")]
    [InlineData("--input-hex", "QZ")]
    [InlineData("--input", "\u00E9")]
    [InlineData("--unknown", "value")]
    public async Task InvalidOptionsReturnFailure(string option, string value)
    {
        var result = await RunCli(Path.Combine(AppContext.BaseDirectory, "Fixtures", "population-count.asm"), option, value);
        Assert.Equal(1, result.Code);
        Assert.NotEmpty(result.Error);
    }

    private static async Task<(int Code, string Output, string Error)> RunCli(params string[] arguments)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "ComputerArchitecture.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        string configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}")
            ? "Release" : "Debug";
        string cli = Path.Combine(root.FullName, "src", "ComputerArchitecture.Mano.Cli", "bin", configuration,
            "net10.0", "ComputerArchitecture.Mano.Cli.dll");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(cli);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start CLI.");
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            return (process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }
}
