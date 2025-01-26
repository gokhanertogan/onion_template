namespace BuildingBlocks.Results;

public class PaginatedResult<T> : Result<IEnumerable<T>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public static PaginatedResult<T> Success(
            IEnumerable<T> data,
            int totalCount,
            int pageNumber, int pageSize,
            int statusCode)
    {
        return new PaginatedResult<T>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            StatusCode = statusCode,
            IsSuccessful = true
        };
    }
}