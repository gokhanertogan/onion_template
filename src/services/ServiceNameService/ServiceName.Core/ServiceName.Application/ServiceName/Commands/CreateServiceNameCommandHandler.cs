using BuildingBlocks.CQRS;
using BuildingBlocks.Results;
using Mapster;
using ServiceName.Application.ServiceName.Common;
using ServiceName.Application.ServiceName.Repositories;

namespace ServiceName.Application.ServiceName.Commands;

public class CreateServiceNameCommandHandler(IServiceNameWriteRepository serviceNameRepository) : ICommandHandler<CreateServiceNameCommand, Result<ServiceNameResult>>
{
    private readonly IServiceNameWriteRepository _serviceNameRepository = serviceNameRepository;
    public async Task<Result<ServiceNameResult>> Handle(CreateServiceNameCommand request, CancellationToken cancellationToken)
    {
        var serviceNameResult = request.Adapt<Domain.Entities.ServiceName>();
        await _serviceNameRepository.AddAsync(serviceNameResult, cancellationToken);

        var addedData = serviceNameResult.Adapt<ServiceNameResult>();
        return Result<ServiceNameResult>.Success(data: addedData, statusCode: 201);
    }
}
