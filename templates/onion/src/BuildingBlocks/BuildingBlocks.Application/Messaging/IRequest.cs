namespace BuildingBlocks.Application.Messaging;

/// <summary>A request that produces <typeparamref name="TResponse"/> when sent through <see cref="ISender"/>.</summary>
#pragma warning disable CA1040 // Marker interface carries the response type.
public interface IRequest<out TResponse>;
#pragma warning restore CA1040
