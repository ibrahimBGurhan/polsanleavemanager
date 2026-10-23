namespace PolsanLeave.SharedKernel;

public sealed class BusinessClock(TimeProvider timeProvider) : IBusinessClock
{
    public static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, BusinessTimeZone).DateTime);
}
