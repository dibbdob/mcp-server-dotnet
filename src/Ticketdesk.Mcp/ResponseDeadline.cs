namespace Ticketdesk.Mcp;

/// <summary>
/// When a ticket's first response is due. Targets are counted in business hours, Monday to Friday,
/// 09:00 to 17:00 UTC, so a ticket opened late on a Friday carries over the weekend.
/// </summary>
public static class ResponseDeadline
{
    public static readonly IReadOnlyDictionary<string, int> Hours = new Dictionary<string, int>
    {
        ["urgent"] = 4, ["high"] = 8, ["normal"] = 24, ["low"] = 40,
    };

    private const int Open = 9;
    private const int Close = 17;

    public static DateTimeOffset DueAt(DateTimeOffset openedAt, string priority)
    {
        if (!Hours.TryGetValue(priority, out var hours))
        {
            throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority.");
        }

        var remaining = TimeSpan.FromHours(hours);
        var now = NextOpening(openedAt.ToUniversalTime());
        while (remaining > TimeSpan.Zero)
        {
            var close = new DateTimeOffset(now.Year, now.Month, now.Day, Close, 0, 0, TimeSpan.Zero);
            var spend = remaining < close - now ? remaining : close - now;
            now += spend;
            remaining -= spend;
            if (remaining > TimeSpan.Zero)
            {
                now = NextOpening(now);
            }
        }

        return now;
    }

    private static bool IsWeekday(DateTimeOffset d) => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static DateTimeOffset NextOpening(DateTimeOffset d)
    {
        var hour = d.Hour + (d.Minute / 60.0);
        if (IsWeekday(d) && hour >= Open && hour < Close)
        {
            return d;
        }

        var next = new DateTimeOffset(d.Year, d.Month, d.Day, Open, 0, 0, TimeSpan.Zero);
        if (hour >= Close || !IsWeekday(d))
        {
            next = next.AddDays(1);
        }

        while (!IsWeekday(next))
        {
            next = next.AddDays(1);
        }

        return next;
    }
}
