using BuildingBlocks.CQRS;
using BuildingBlocks.Result;
using Mapster;
using MediatR;
using ProjectName.Application.ProjectName.Common;
using ProjectName.Application.ProjectName.Repositories;

namespace ProjectName.Application.ProjectName.Commands;

public class CreateProjectNameCommandHandler(IProjectNameWriteRepository projectNameWriteRepository) : ICommandHandler<CreatePropertyCommand, Result<ProjectNameResult>>
{
    private readonly IProjectNameWriteRepository _projectNameWriteRepository = projectNameWriteRepository;
    public async Task<Result<ProjectNameResult>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var projectNameResult = request.Adapt<Domain.Entities.ProjectName>();
        await _projectNameWriteRepository.AddAsync(projectNameResult, cancellationToken);

        var addedData = projectNameResult.Adapt<ProjectNameResult>();
        return Result<ProjectNameResult>.Success(data: addedData, statusCode: 201);
    }
}
