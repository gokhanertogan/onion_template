namespace BuildingBlocks.Tests;

using BuildingBlocks.Application.Validation;

[TestClass]
public sealed class ValidatorTests
{
    [TestMethod]
    public async Task Valid_Instance_HasNoErrors()
    {
        var result = await new PersonValidator().ValidateAsync(new Person("Ada", 36, "ada@example.com", new Address("Main"), ["dev"]));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Invalid_Instance_ReportsEveryFailure()
    {
        var result = await new PersonValidator().ValidateAsync(new Person("", 200, "nope", new Address(""), ["ok", ""]));

        CollectionAssert.AreEquivalent(
            new[] { "Name", "Age", "Email", "Address.Street", "Tags[1]" },
            result.Errors.Select(e => e.PropertyName).ToArray());
    }

    [TestMethod]
    public async Task ErrorCodes_AndMessages_AreReadable()
    {
        var result = await new PersonValidator().ValidateAsync(new Person("", 1, null, null, []));
        var error = result.ToValidationError().Errors.Single();

        Assert.AreEqual("Name.NotEmpty", error.Code);
        Assert.AreEqual("'Name' must not be empty.", error.Description);
    }

    [TestMethod]
    public async Task When_SkipsRule_IfConditionIsFalse()
    {
        var result = await new PersonValidator().ValidateAsync(new Person("Bob", 10, "invalid-but-minor", null, []));

        Assert.IsTrue(result.IsValid);
    }

    public sealed record Address(string Street);

    public sealed record Person(string Name, int Age, string? Email, Address? Address, IReadOnlyList<string> Tags);

    private sealed class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator() => RuleFor(x => x.Street).NotEmpty();
    }

    private sealed class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Age).InclusiveBetween(0, 150);
            RuleFor(x => x.Email).EmailAddress().When(x => x.Age >= 18);
            RuleFor(x => x.Address).SetValidator(new AddressValidator());
            RuleForEach(x => x.Tags).NotEmpty();
        }
    }
}
