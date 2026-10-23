using PolsanLeave.SharedKernel;

namespace PolsanLeave.Calendar.Contracts;

/// <summary>Working-day arithmetic (BR-3): Saturdays, Sundays and public holidays are not working days.</summary>
public interface IWorkingDayService
{
    /// <summary>
    /// Working days from <paramref name="start"/> to <paramref name="end"/>, both inclusive.
    /// Fails with <see cref="CalendarErrors.NotDefined"/> when any year in the range has no holiday calendar.
    /// </summary>
    Task<Result<int>> CountWorkingDaysAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}
