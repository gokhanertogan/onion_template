using BuildingBlocks.Result;
using MediatR;

namespace BuildingBlocks.CQRS;
public interface IQuery<TResponse> : IRequest<TResponse>
{
}