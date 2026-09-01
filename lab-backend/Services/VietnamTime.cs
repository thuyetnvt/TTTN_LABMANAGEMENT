namespace LabManagementAPI.Services;

/// <summary>
/// Business dates in the lab are evaluated in Vietnam time (UTC+7), while
/// timestamps continue to be persisted in UTC. Vietnam does not observe DST,
/// so a fixed offset is deterministic on both Windows and Linux containers.
/// </summary>
public static class VietnamTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime Now(DateTime? utcNow = null)
        => AsUtc(utcNow ?? DateTime.UtcNow).Add(Offset);

    public static DateTime Today(DateTime? utcNow = null)
        => Now(utcNow).Date;

    public static DateTime Date(DateTime utcValue)
        => AsUtc(utcValue).Add(Offset).Date;

    public static DateTime StartOfDayUtc(DateTime vietnamDate)
        => DateTime.SpecifyKind(vietnamDate.Date.Subtract(Offset), DateTimeKind.Utc);

    private static DateTime AsUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
