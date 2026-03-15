namespace BakeryFlow.Application.Common.Time;

public static class UtcDateTime
{
    public static DateTime EnsureUtc(DateTime value)
    {
        if (value == default)
        {
            return default;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    public static DateTime? EnsureUtc(DateTime? value) =>
        value.HasValue ? EnsureUtc(value.Value) : null;

    public static DateTime StartOfDay(DateTime value)
    {
        var utc = EnsureUtc(value);
        return new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime StartOfMonth(DateTime value)
    {
        var utc = EnsureUtc(value);
        return new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
