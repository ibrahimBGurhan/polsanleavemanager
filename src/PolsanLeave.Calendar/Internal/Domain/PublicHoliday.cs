namespace PolsanLeave.Calendar.Internal.Domain;

/// <summary>A full-day official public holiday (2464 sayılı Kanun). Half-day arife afternoons are not holidays here.</summary>
internal sealed record PublicHoliday(DateOnly Date, string Name);

/// <summary>A year whose holiday calendar is defined; requests touching other years are rejected (AC-40).</summary>
internal sealed record CalendarYear(int Year);
