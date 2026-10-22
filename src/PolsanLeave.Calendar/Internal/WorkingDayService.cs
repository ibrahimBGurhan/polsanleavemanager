using Microsoft.EntityFrameworkCore;
using PolsanLeave.Calendar.Contracts;
using PolsanLeave.Calendar.Internal.Data;
using PolsanLeave.Calendar.Internal.Domain;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Calendar.Internal;

internal sealed class WorkingDayService(CalendarDbContext db) : IWorkingDayService
{
    public async Task<Result<int>> CountWorkingDaysAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        var years = await db.CalendarYears.AsNoTracking()
            .Where(y => y.Year >= start.Year && y.Year <= end.Year)
            .Select(y => y.Year)
            .ToListAsync(cancellationToken);
        var holidays = await db.PublicHolidays.AsNoTracking()
            .Where(h => h.Date >= start && h.Date <= end)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        return new WorkingDayCalculator(holidays.ToHashSet(), years.ToHashSet()).Count(start, end);
    }
}
