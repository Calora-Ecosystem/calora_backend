namespace Core.Helpers;

/// <summary>
/// Dashboard davri: <c>from</c> kun boshidan <c>to</c> kun oxirigacha (ikkala kun ham kiradi).
/// Berilmasa — <paramref name="defaultStart"/>dan bugungacha.
/// </summary>
public static class DashboardPeriod
{
    public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime defaultStart)
    {
        var start = (from ?? defaultStart).Date;
        var end = (to ?? DateTime.Now).Date;
        if (end < start) (start, end) = (end, start);

        return (start, end.AddDays(1).AddTicks(-10));
    }
}
