using BuildingBlocks.CQRS;
using BuildingBlocks.Results;
using ServiceName.Application.ServiceName.Common;

namespace ServiceName.Application.ServiceName.Commands;

public record CreateServiceNameCommand(string Name) : ICommand<Result<ServiceNameResult>>;