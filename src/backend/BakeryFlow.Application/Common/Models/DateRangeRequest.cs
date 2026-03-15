namespace BakeryFlow.Application.Common.Models;

public sealed class DateRangeRequest
{
    public DateTime? From { get; init; }

    public DateTime? To { get; init; }
}
