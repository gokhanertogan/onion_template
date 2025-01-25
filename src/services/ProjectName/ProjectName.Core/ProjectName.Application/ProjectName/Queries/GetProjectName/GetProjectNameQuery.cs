using BuildingBlocks.CQRS;
using BuildingBlocks.Result;
using ProjectName.Application.ProjectName.Common;

namespace ProjectName.Application.ProjectName.Queries.GetProjectName;

public record GetProjectNameQuery(PaginationRequest PaginationRequest) 
    : IQuery<PaginatedResult<ProjectNameResult>>;