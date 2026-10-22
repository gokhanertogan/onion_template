namespace ServiceName.ArchitectureTests;

using System.Reflection;

/// <summary>
/// Enforces the onion dependency rule with plain reflection (no third-party library):
/// dependencies point inwards only, Domain ← Application ← Infrastructure/Persistence ← API.
/// </summary>
[TestClass]
public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(global::ServiceName.Domain.ServiceNames.ServiceNameId).Assembly;
    private static readonly Assembly Application = typeof(global::ServiceName.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(global::ServiceName.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Persistence = typeof(global::ServiceName.Persistence.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;
    private static readonly Assembly Contracts = typeof(global::ServiceName.Contracts.PagedResponse<>).Assembly;

    [TestMethod]
    public void Domain_DependsOnNothingButTheDomainKernel()
    {
        AssertNoDependency(Domain, Application, Infrastructure, Persistence, Api, Contracts);
        AssertNoDependencyOnPrefix(Domain, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Extensions", "BuildingBlocks.Application", "BuildingBlocks.Infrastructure", "BuildingBlocks.Web");
    }

    [TestMethod]
    public void Application_DoesNotDependOnOuterLayers()
    {
        AssertNoDependency(Application, Infrastructure, Persistence, Api);
        AssertNoDependencyOnPrefix(Application, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "BuildingBlocks.Infrastructure", "BuildingBlocks.Web");
    }

    [TestMethod]
    public void InfrastructureAndPersistence_DoNotDependOnApi()
    {
        AssertNoDependency(Infrastructure, Api);
        AssertNoDependency(Persistence, Api, Infrastructure);
        AssertNoDependencyOnPrefix(Persistence, "Microsoft.AspNetCore", "BuildingBlocks.Web");
    }

    [TestMethod]
    public void Contracts_HaveNoDependencies()
    {
        var references = Contracts.GetReferencedAssemblies().Select(a => a.Name!).Where(n => !IsFramework(n)).ToArray();

        Assert.IsEmpty(references, $"Contracts must be dependency free but references: {string.Join(", ", references)}");
    }

    private static void AssertNoDependency(Assembly assembly, params Assembly[] forbidden)
    {
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet();
        foreach (var target in forbidden)
        {
            Assert.DoesNotContain(target.GetName().Name, references, $"{assembly.GetName().Name} must not depend on {target.GetName().Name}.");
        }
    }

    private static void AssertNoDependencyOnPrefix(Assembly assembly, params string[] prefixes)
    {
        var offending = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => prefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.IsEmpty(offending, $"{assembly.GetName().Name} must not reference: {string.Join(", ", offending)}");
    }

    private static bool IsFramework(string name) =>
        name is "netstandard" or "mscorlib" || name.StartsWith("System", StringComparison.Ordinal);
}
