public static class GlobalConstants
{
    public static readonly Assembly CurrentAssembly = typeof(GlobalConstants).Assembly;
    public static readonly string CurrentAssemblyName = CurrentAssembly.GetName().Name ?? throw new InvalidOperationException("Failed to get current assembly name.");

    public static class Directories
    {
        public static DirectoryInfo TestData { get; } = new(Path.Combine(AppContext.BaseDirectory, "TestData"));
    }
}