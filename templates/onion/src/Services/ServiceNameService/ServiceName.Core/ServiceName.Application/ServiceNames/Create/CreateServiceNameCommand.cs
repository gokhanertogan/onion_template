namespace ServiceName.Application.ServiceNames.Create;

using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Results;
using ServiceName.Domain.ServiceNames;

public sealed record CreateServiceNameCommand(string Name, string? Description, Guid? IdempotencyKey = null)
    : ICommand<Guid>, IIdempotentCommand, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => ServiceNameCacheKeys.Tags;
}

internal sealed class CreateServiceNameCommandValidator : AbstractValidator<CreateServiceNameCommand>
{
    public CreateServiceNameCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ServiceName.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ServiceName.DescriptionMaxLength);
    }
}

internal sealed class CreateServiceNameCommandHandler(IServiceNameRepository repository)
    : ICommandHandler<CreateServiceNameCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateServiceNameCommand command, CancellationToken cancellationToken)
    {
        if (await repository.NameExistsAsync(command.Name.Trim(), cancellationToken: cancellationToken))
        {
            return ServiceNameErrors.NameAlreadyExists(command.Name.Trim());
        }

        var created = ServiceName.Create(command.Name, command.Description);
        if (created.IsFailure)
        {
            return created.Error;
        }

        repository.Add(created.Value);
        return created.Value.Id.Value;
    }
}
