namespace ServiceName.UnitTests.Application;

using BuildingBlocks.Domain.Results;
using ServiceName.Application.ServiceNames.Create;
using ServiceName.Domain.ServiceNames;

[TestClass]
public sealed class CreateServiceNameCommandTests
{
    [TestMethod]
    public async Task Handle_AddsAggregate_AndReturnsId()
    {
        var repository = new FakeServiceNameRepository();
        var handler = new CreateServiceNameCommandHandler(repository);

        var result = await handler.Handle(new CreateServiceNameCommand("First", null), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(result.Value, repository.Items.Single().Id.Value);
    }

    [TestMethod]
    public async Task Handle_ReturnsConflict_WhenNameExists()
    {
        var repository = new FakeServiceNameRepository();
        repository.Add(ServiceName.Create("First", null).Value);
        var handler = new CreateServiceNameCommandHandler(repository);

        var result = await handler.Handle(new CreateServiceNameCommand(" First ", null), CancellationToken.None);

        Assert.AreEqual(ErrorType.Conflict, result.Error.Type);
        Assert.HasCount(1, repository.Items);
    }

    [TestMethod]
    public async Task Validator_RejectsEmptyAndTooLongValues()
    {
        var validator = new CreateServiceNameCommandValidator();

        var result = await validator.ValidateAsync(new CreateServiceNameCommand("", new string('x', ServiceName.DescriptionMaxLength + 1)));

        CollectionAssert.AreEquivalent(new[] { "Name", "Description" }, result.Errors.Select(e => e.PropertyName).ToArray());
    }
}
