using MediatR;

namespace BuildingBlocks.CQRS;
public interface ICommand<TResponse> : IRequest<TResponse>
{
}