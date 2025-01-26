using BuildingBlocks.CQRS;
using BuildingBlocks.Results;
using ProjectName.Application.ProjectName.Common;

namespace ProjectName.Application.ProjectName.Queries.GetProjectName;

public record GetProjectNameQuery(string Name, PaginationRequest PaginationRequest) 
    : IQuery<PaginatedResult<ProjectNameResult>>;