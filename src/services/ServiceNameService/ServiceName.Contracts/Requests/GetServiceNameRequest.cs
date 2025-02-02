namespace ServiceName.Contracts.Requests;

public record GetServiceNameRequest(string Name, int PageIndex = 0, int PageSize = 10) : BaseRequest;

