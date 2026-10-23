using Microsoft.Extensions.Localization;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Web.Components.Shared;

/// <summary>Maps protocol values to Turkish UI texts (Resources/Messages.resx).</summary>
internal static class Texts
{
    public static string For(this IStringLocalizer<Messages> l, Error error) => l[error.Code];

    public static string For(this IStringLocalizer<Messages> l, LeaveRequestStatus status) => l["Status." + status];

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

    public static string LocalDateTime(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BusinessClock.BusinessTimeZone).ToString("dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
