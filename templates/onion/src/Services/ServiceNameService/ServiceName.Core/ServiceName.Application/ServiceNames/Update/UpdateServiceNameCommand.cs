namespace ServiceName.Application.ServiceNames.Update;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Results;
using ServiceName.Domain.ServiceNames;

public sealed record UpdateServiceNameCommand(Guid Id, string Name, string? Description) : ICommand, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => ServiceNameCacheKeys.Tags;
}

internal sealed class UpdateServiceNameCommandValidator : AbstractValidator<UpdateServiceNameCommand>
{
    public UpdateServiceNameCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ServiceName.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ServiceName.DescriptionMaxLength);
    }
}

internal sealed class UpdateServiceNameCommandHandler(IServiceNameRepository repository) : ICommandHandler<UpdateServiceNameCommand>
{
    public async Task<Result> Handle(UpdateServiceNameCommand command, CancellationToken cancellationToken)
    {
        var id = new ServiceNameId(command.Id);
        var serviceName = await repository.GetByIdAsync(id, cancellationToken);
        if (serviceName is null)
        {
            return ServiceNameErrors.NotFound(id);
        }

        if (await repository.NameExistsAsync(command.Name.Trim(), id, cancellationToken))
        {
            return ServiceNameErrors.NameAlreadyExists(command.Name.Trim());
        }

        return serviceName.Update(command.Name, command.Description);
    }
}
