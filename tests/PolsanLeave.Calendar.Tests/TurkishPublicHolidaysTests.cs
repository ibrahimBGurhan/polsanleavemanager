using PolsanLeave.Calendar.Internal.Data;

namespace PolsanLeave.Calendar.Tests;

public class TurkishPublicHolidaysTests
{
    [Fact]
    public void All_EveryHolidayFallsInADefinedYear() =>
        Assert.All(TurkishPublicHolidays.All, h => Assert.Contains(TurkishPublicHolidays.Years, y => y.Year == h.Date.Year));

    [Fact]
    public void All_DatesAreUnique() =>
        Assert.Equal(TurkishPublicHolidays.All.Length, TurkishPublicHolidays.All.Select(h => h.Date).Distinct().Count());

    [Theory]
    [InlineData(2026, 14)] // 7 fixed + 3 Ramazan + 4 Kurban
    [InlineData(2027, 13)] // 19 May 2027 is both Kurban 4th day and 19 Mayıs
    public void All_HasExpectedHolidayCountPerYear(int year, int expected) =>
        Assert.Equal(expected, TurkishPublicHolidays.All.Count(h => h.Date.Year == year));
}
