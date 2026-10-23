using PolsanLeave.Calendar.Contracts;
using PolsanLeave.Calendar.Internal.Data;
using PolsanLeave.Calendar.Internal.Domain;

namespace PolsanLeave.Calendar.Tests;

/// <summary>BR-3 against the real 2026–2027 seed data (spec 0001, AC-17 … AC-21, AC-40).</summary>
public class WorkingDayCalculatorTests
{
    private static readonly WorkingDayCalculator Calculator = new(
        TurkishPublicHolidays.All.Select(h => h.Date).ToHashSet(),
        TurkishPublicHolidays.Years.Select(y => y.Year).ToHashSet());

    [Fact]
    public void Count_MonToFri_NoHolidays_Is5() =>
        Assert.Equal(5, Calculator.Count(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6)).Value);

    [Fact]
    public void Count_FriToMon_Is2() =>
        Assert.Equal(2, Calculator.Count(new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 9)).Value);

    [Fact]
    public void Count_Oct26To30_2026_With29OctHoliday_Is4() =>
        Assert.Equal(4, Calculator.Count(new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 30)).Value);

    [Theory]
    [InlineData(2026, 5, 26)] // Kurban Bayramı arifesi (Tuesday)
    [InlineData(2026, 10, 28)] // Cumhuriyet Bayramı arifesi (Wednesday)
    [InlineData(2027, 3, 8)] // Ramazan Bayramı arifesi (Monday)
    public void Count_ArifeDay_IsWorkingDay(int year, int month, int day)
    {
        var arife = new DateOnly(year, month, day);

        Assert.Equal(1, Calculator.Count(arife, arife).Value);
    }

    [Fact]
    public void Count_SingleWorkingDay_Is1() =>
        Assert.Equal(1, Calculator.Count(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4)).Value);

    [Fact]
    public void Count_KurbanBayrami2026Week_CountsOnlyArifeAndMonday()
    {
        // Mon 25 May .. Sun 31 May 2026: 25 (Mon) and 26 (arife) work; 27–30 holiday; 31 Sunday.
        Assert.Equal(2, Calculator.Count(new DateOnly(2026, 5, 25), new DateOnly(2026, 5, 31)).Value);
    }

    [Fact]
    public void Coverage_RangeInto2028_ReportsMissingCalendar()
    {
        var result = Calculator.Count(new DateOnly(2027, 12, 30), new DateOnly(2028, 1, 3));

        Assert.False(result.IsSuccess);
        Assert.Equal(CalendarErrors.NotDefined, result.Error);
    }

    [Fact]
    public void Count_EndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() => Calculator.Count(new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 5)));
}
