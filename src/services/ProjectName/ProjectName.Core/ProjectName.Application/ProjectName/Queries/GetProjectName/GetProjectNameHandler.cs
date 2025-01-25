using BuildingBlocks.CQRS;
using BuildingBlocks.Result;
using Mapster;
using ProjectName.Application.ProjectName.Common;
using ProjectName.Application.ProjectName.Repositories;

namespace ProjectName.Application.ProjectName.Queries.GetProjectName;
public class GetProjectNameHandler(IProjectNameReadRepository projectNameReadRepository) : IQueryHandler<GetProjectNameQuery, PaginatedResult<ProjectNameResult>>
{
    private readonly IProjectNameReadRepository _projectNameReadRepository = projectNameReadRepository;

    public async Task<PaginatedResult<ProjectNameResult>> Handle(GetProjectNameQuery request, CancellationToken cancellationToken)
    {
        var totalItemCount = await _projectNameReadRepository.CountAsync(cancellationToken);
        var items = await _projectNameReadRepository.GetAllPaginatedAsync(request.PaginationRequest, cancellationToken);

        return PaginatedResult<ProjectNameResult>.Success(data: items.Adapt<List<ProjectNameResult>>(),
                                                          totalCount: totalItemCount,
                                                          pageNumber: request.PaginationRequest.PageIndex,
                                                          pageSize: request.PaginationRequest.PageSize,
                                                          statusCode: 200);
    }
}