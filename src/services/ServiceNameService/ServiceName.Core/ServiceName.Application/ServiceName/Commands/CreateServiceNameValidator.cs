using FluentValidation;
namespace ServiceName.Application.ServiceName.Commands;
public class CreateServiceNameValidator : AbstractValidator<CreateServiceNameCommand>
{
    public CreateServiceNameValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}