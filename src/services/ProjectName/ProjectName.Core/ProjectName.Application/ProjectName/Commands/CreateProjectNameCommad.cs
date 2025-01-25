using BuildingBlocks.CQRS;
using BuildingBlocks.Result;
using ProjectName.Application.ProjectName.Common;

namespace ProjectName.Application.ProjectName.Commands;

public record CreatePropertyCommand(string Name) : ICommand<Result<ProjectNameResult>>;