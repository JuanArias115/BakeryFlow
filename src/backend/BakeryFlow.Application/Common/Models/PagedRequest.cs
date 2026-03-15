namespace BakeryFlow.Application.Common.Models;

public class PagedRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Search { get; init; }
}
