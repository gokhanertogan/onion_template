namespace BuildingBlocks.Tests;

using System.Text.Json;
using BuildingBlocks.Domain.Results;

[TestClass]
public sealed class ResultTests
{
    [TestMethod]
    public void Success_HasValue_AndNoError()
    {
        Result<int> result = 42;

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(42, result.Value);
        Assert.AreEqual(Error.None, result.Error);
    }

    [TestMethod]
    public void Failure_ValueAccess_Throws()
    {
        Result<int> result = Error.NotFound("X.NotFound", "missing");

        Assert.IsTrue(result.IsFailure);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    [TestMethod]
    public void Combine_ReturnsValidationError_WhenSeveralFail()
    {
        var combined = Result.Combine(
            Result.Success(),
            Error.Validation("A", "a"),
            Error.Validation("B", "b"));

        var validation = combined.Error as ValidationError;
        Assert.IsNotNull(validation);
        Assert.HasCount(2, validation.Errors);
    }

    [TestMethod]
    public void SuccessResult_RoundTripsThroughJson()
    {
        var json = JsonSerializer.Serialize(Result.Success(new Sample("a", 1)));
        var restored = JsonSerializer.Deserialize<Result<Sample>>(json);

        Assert.IsNotNull(restored);
        Assert.IsTrue(restored.IsSuccess);
        Assert.AreEqual(new Sample("a", 1), restored.Value);
    }

    public sealed record Sample(string Name, int Count);
}
