using BuildingBlocks.Result;
using MediatR;

namespace BuildingBlocks.CQRS;
public interface IQueryHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : IQuery<TResponse>
{
}