namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Commands carrying a client supplied idempotency key (usually the <c>Idempotency-Key</c> header).
/// Repeated requests with the same key return the first successful result instead of executing again.
/// </summary>
public interface IIdempotentCommand : IBaseCommand
{
    Guid? IdempotencyKey { get; }
}
