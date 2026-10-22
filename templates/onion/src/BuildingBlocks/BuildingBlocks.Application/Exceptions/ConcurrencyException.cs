namespace BuildingBlocks.Application.Exceptions;

/// <summary>Thrown when an optimistic concurrency check fails. Mapped to HTTP 409.</summary>
public sealed class ConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
