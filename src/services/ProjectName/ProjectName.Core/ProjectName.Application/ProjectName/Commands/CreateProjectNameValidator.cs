using FluentValidation;
namespace ProjectName.Application.ProjectName.Commands;
public class CreateProjectNameValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreateProjectNameValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}