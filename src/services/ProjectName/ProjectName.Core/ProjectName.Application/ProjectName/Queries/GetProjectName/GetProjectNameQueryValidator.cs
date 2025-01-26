using BuildingBlocks.Results;
using FluentValidation;

namespace ProjectName.Application.ProjectName.Queries.GetProjectName;
public class GetProjectNameQueryValidator : AbstractValidator<GetProjectNameQuery>
{
    public GetProjectNameQueryValidator()
    {
        RuleFor(query => query.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(query => query.PaginationRequest)
            .NotNull().WithMessage("PaginationRequest cannot be null.")
            .SetValidator(new PaginationRequestValidator());
    }
}

public class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        RuleFor(request => request.PageIndex)
            .GreaterThanOrEqualTo(0).WithMessage("PageIndex must be greater than or equal to 0.");

        RuleFor(request => request.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100.");
    }
}