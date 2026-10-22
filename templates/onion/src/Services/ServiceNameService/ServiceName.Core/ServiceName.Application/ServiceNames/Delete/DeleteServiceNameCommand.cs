namespace ServiceName.Application.ServiceNames.Delete;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using ServiceName.Domain.ServiceNames;

public sealed record DeleteServiceNameCommand(Guid Id) : ICommand, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => ServiceNameCacheKeys.Tags;
}

internal sealed class DeleteServiceNameCommandHandler(IServiceNameRepository repository) : ICommandHandler<DeleteServiceNameCommand>
{
    public async Task<Result> Handle(DeleteServiceNameCommand command, CancellationToken cancellationToken)
    {
        var id = new ServiceNameId(command.Id);
        var serviceName = await repository.GetByIdAsync(id, cancellationToken);
        if (serviceName is null)
        {
            return ServiceNameErrors.NotFound(id);
        }

        serviceName.MarkDeleted();
        repository.Remove(serviceName);
        return Result.Success();
    }
}
