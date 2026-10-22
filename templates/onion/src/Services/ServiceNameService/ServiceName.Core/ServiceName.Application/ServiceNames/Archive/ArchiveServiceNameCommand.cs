namespace ServiceName.Application.ServiceNames.Archive;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Results;
using ServiceName.Domain.ServiceNames;

public sealed record ArchiveServiceNameCommand(Guid Id) : ICommand, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => ServiceNameCacheKeys.Tags;
}

internal sealed class ArchiveServiceNameCommandHandler(IServiceNameRepository repository) : ICommandHandler<ArchiveServiceNameCommand>
{
    public async Task<Result> Handle(ArchiveServiceNameCommand command, CancellationToken cancellationToken)
    {
        var id = new ServiceNameId(command.Id);
        var serviceName = await repository.GetByIdAsync(id, cancellationToken);

        return serviceName is null ? ServiceNameErrors.NotFound(id) : serviceName.Archive();
    }
}
