namespace BuildingBlocks.Domain.Results;

/// <summary>
/// Lets generic code (e.g. pipeline behaviors) create a failed result of any
/// result type without reflection, using static abstract interface members.
/// </summary>
public interface IResultFactory<out TSelf>
    where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf Failure(Error error);
}
