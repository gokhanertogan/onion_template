namespace ServiceName.Contracts.ServiceNames;

public sealed record CreateServiceNameRequest(string Name, string? Description);

public sealed record UpdateServiceNameRequest(string Name, string? Description);

public sealed record ServiceNameResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc);

public sealed record CreatedResponse(Guid Id);
