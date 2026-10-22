namespace ServiceName.ArchitectureTests;

using System.Reflection;
using BuildingBlocks.Application.Events;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Web.Endpoints;

[TestClass]
public sealed class ConventionTests
{
    private static readonly Assembly Domain = typeof(global::ServiceName.Domain.ServiceNames.ServiceNameId).Assembly;
    private static readonly Assembly Application = typeof(global::ServiceName.Application.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [TestMethod]
    public void Handlers_AndValidators_AreInternalAndSealed()
    {
        var types = Application.GetTypes().Where(t => !t.IsAbstract && (
            Implements(t, typeof(IRequestHandler<,>)) ||
            Implements(t, typeof(IDomainEventHandler<>)) ||
            Implements(t, typeof(IValidator<>))));

        AssertAll(types, t => t.IsSealed && !t.IsPublic, "must be internal sealed");
    }

    [TestMethod]
    public void Endpoints_AreInternalAndSealed() =>
        AssertAll(Api.GetTypes().Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface), t => t.IsSealed && !t.IsPublic, "must be internal sealed");

    [TestMethod]
    public void DomainEvents_AreSealedRecords() =>
        AssertAll(
            Domain.GetTypes().Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsAbstract),
            t => t.IsSealed && t.GetMethod("<Clone>$") is not null,
            "must be sealed records (immutable)");

    [TestMethod]
    public void Aggregates_HaveNoPublicSetters() =>
        AssertAll(
            Domain.GetTypes().Where(t => IsSubclassOfGeneric(t, typeof(AggregateRoot<>))),
            t => t.GetProperties().All(p => p.SetMethod is null || !p.SetMethod.IsPublic),
            "must not expose public setters (encapsulate state changes in methods)");

    [TestMethod]
    public void Aggregates_AreSealed_AndHavePrivateConstructors() =>
        AssertAll(
            Domain.GetTypes().Where(t => IsSubclassOfGeneric(t, typeof(AggregateRoot<>))),
            t => t.IsSealed && t.GetConstructors().Length == 0,
            "must be sealed and created through factory methods");

    private static bool Implements(Type type, Type openInterface) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterface);

    private static bool IsSubclassOfGeneric(Type type, Type openBase)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openBase)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertAll(IEnumerable<Type> types, Func<Type, bool> rule, string message)
    {
        var list = types.ToList();
        Assert.IsNotEmpty(list, "Rule matched no types; is the selector still correct?");

        var violations = list.Where(t => !rule(t)).Select(t => t.FullName).ToArray();
        Assert.IsEmpty(violations, $"Types {message}: {string.Join(", ", violations)}");
    }
}
