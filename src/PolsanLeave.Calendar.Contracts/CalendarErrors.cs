using PolsanLeave.SharedKernel;

namespace PolsanLeave.Calendar.Contracts;

public static class CalendarErrors
{
    /// <summary>The holiday calendar of a year in the range is not defined (spec 0001, AC-40).</summary>
    public static readonly Error NotDefined = new("Calendar.NotDefined");
}
