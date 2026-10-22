using PolsanLeave.Calendar.Contracts;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Calendar.Internal.Domain;

/// <summary>BR-3: counts days that are neither Saturday, Sunday nor a public holiday. Arife days are working days.</summary>
internal sealed class WorkingDayCalculator(IReadOnlySet<DateOnly> holidays, IReadOnlySet<int> definedYears)
{
    public Result<int> Count(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("End date is before start date; callers validate the range first.", nameof(end));
        }

        for (var year = start.Year; year <= end.Year; year++)
        {
            if (!definedYears.Contains(year))
            {
                return CalendarErrors.NotDefined;
            }
        }

        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(day))
            {
                count++;
            }
        }

        return count;
    }
}
