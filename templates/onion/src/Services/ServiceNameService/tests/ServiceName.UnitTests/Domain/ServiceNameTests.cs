namespace ServiceName.UnitTests.Domain;

using BuildingBlocks.Domain.Results;
using ServiceName.Domain.ServiceNames;

[TestClass]
public sealed class ServiceNameTests
{
    [TestMethod]
    public void Create_WithValidData_RaisesCreatedEvent()
    {
        var result = ServiceName.Create("  First  ", "  description ");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("First", result.Value.Name);
        Assert.AreEqual("description", result.Value.Description);
        Assert.AreEqual(ServiceNameStatus.Active, result.Value.Status);

        var domainEvent = result.Value.DomainEvents.OfType<ServiceNameCreatedDomainEvent>().Single();
        Assert.AreEqual(result.Value.Id, domainEvent.ServiceNameId);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_WithoutName_Fails(string name)
    {
        var result = ServiceName.Create(name, null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ServiceNameErrors.NameRequired, result.Error);
    }

    [TestMethod]
    public void Create_WithTooLongName_Fails()
    {
        var result = ServiceName.Create(new string('x', ServiceName.NameMaxLength + 1), null);

        Assert.AreEqual(ServiceNameErrors.NameTooLong, result.Error);
    }

    [TestMethod]
    public void Update_ChangesState_AndRaisesEvent()
    {
        var serviceName = ServiceName.Create("First", null).Value;
        serviceName.ClearDomainEvents();

        var result = serviceName.Update("Second", "desc");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Second", serviceName.Name);
        Assert.IsInstanceOfType<ServiceNameUpdatedDomainEvent>(serviceName.DomainEvents.Single());
    }

    [TestMethod]
    public void Archive_Twice_ReturnsConflict()
    {
        var serviceName = ServiceName.Create("First", null).Value;

        Assert.IsTrue(serviceName.Archive().IsSuccess);
        var second = serviceName.Archive();

        Assert.AreEqual(ErrorType.Conflict, second.Error.Type);
    }
}
