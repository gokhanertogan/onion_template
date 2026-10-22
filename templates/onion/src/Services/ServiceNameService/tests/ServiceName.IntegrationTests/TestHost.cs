namespace ServiceName.IntegrationTests;

/// <summary>One API host for the whole test assembly (fast; tests isolate themselves with unique data).</summary>
[TestClass]
public static class TestHost
{
    public static ServiceNameApiFactory Factory { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        Factory = new ServiceNameApiFactory();
        await Factory.InitializeDatabaseAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync() => await Factory.DisposeAsync();
}
