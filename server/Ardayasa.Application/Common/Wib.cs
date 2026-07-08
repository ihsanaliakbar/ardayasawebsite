namespace Ardayasa.Application.Common;

/// <summary>
/// WIB (Asia/Jakarta) conversions. Always go through <see cref="TimeZoneInfo"/> —
/// never a hardcoded +7 (project invariant). WIB has no DST, but the IANA rules
/// remain the single source of truth.
/// </summary>
public static class Wib
{
    public static readonly TimeZoneInfo TimeZone = FindTimeZone();

    private static TimeZoneInfo FindTimeZone()
        => TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Jakarta", out var tz)
            ? tz
            // Windows hosts without ICU fall back to the legacy id for UTC+7.
            : TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static DateTime ToUtc(DateOnly date, TimeOnly time)
        => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time, DateTimeKind.Unspecified), TimeZone);

    public static DateTime ToWib(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);

    public static DateOnly ToWibDate(DateTime utc) => DateOnly.FromDateTime(ToWib(utc));

    public static DateOnly Today(DateTime nowUtc) => ToWibDate(nowUtc);
}
