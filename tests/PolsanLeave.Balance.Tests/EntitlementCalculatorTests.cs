using System.Globalization;
using PolsanLeave.Balance.Internal.Domain;

namespace PolsanLeave.Balance.Tests;

/// <summary>BR-1, BR-2, BR-9 (spec 0001, AC-6 … AC-14, AC-16).</summary>
public class EntitlementCalculatorTests
{
    private static readonly DateOnly Opening = new(2026, 1, 1);

    /// <summary>Accrual at the anniversary that completes <paramref name="seniority"/> years, at the given age.</summary>
    private static int DaysAt(int seniority, int age)
    {
        var anniversary = new DateOnly(2026, 11, 10);
        var hire = anniversary.AddYears(-seniority);
        var birth = anniversary.AddYears(-age);

        return Assert.Single(EntitlementCalculator.Accruals(hire, birth, Opening, anniversary)).Days;
    }

    [Fact]
    public void Entitlement_FirstAnniversaryAfterOpening_Adds14()
    {
        var accrual = Assert.Single(EntitlementCalculator.Accruals(
            new DateOnly(2025, 11, 10), new DateOnly(1996, 1, 1), Opening, new DateOnly(2026, 11, 10)));

        Assert.Equal(new DateOnly(2026, 11, 10), accrual.Anniversary);
        Assert.Equal(1, accrual.SeniorityYears);
        Assert.Equal(14, accrual.Days);
    }

    [Fact]
    public void Entitlement_Seniority5_Age30_Is14() => Assert.Equal(14, DaysAt(5, 30));

    [Fact]
    public void Entitlement_Seniority6_Age30_Is20() => Assert.Equal(20, DaysAt(6, 30));

    [Fact]
    public void Entitlement_Seniority14_Age40_Is20() => Assert.Equal(20, DaysAt(14, 40));

    [Fact]
    public void Entitlement_Seniority15_Age40_Is26() => Assert.Equal(26, DaysAt(15, 40));

    [Fact]
    public void Entitlement_Seniority3_Age18_Is20() => Assert.Equal(20, DaysAt(3, 18));

    [Fact]
    public void Entitlement_Seniority3_Age19_Is14() => Assert.Equal(14, DaysAt(3, 19));

    [Fact]
    public void Entitlement_Seniority3_Age50_Is20() => Assert.Equal(20, DaysAt(3, 50));

    [Fact]
    public void Entitlement_Seniority3_Age49_Is14() => Assert.Equal(14, DaysAt(3, 49));

    [Fact]
    public void Entitlement_Seniority20_Age55_Is26() => Assert.Equal(26, DaysAt(20, 55));

    [Fact]
    public void Entitlement_BeforeFirstAnniversary_IsZero() =>
        Assert.Empty(EntitlementCalculator.Accruals(
            new DateOnly(2026, 3, 1), new DateOnly(1995, 1, 1), Opening, new DateOnly(2027, 2, 28)));

    [Fact]
    public void Entitlement_AnniversariesBeforeOpening_NotAccrued()
    {
        // Hired 2015-03-01: anniversaries 2016 … 2025 precede the opening date; only 2026 accrues.
        var accrual = Assert.Single(EntitlementCalculator.Accruals(
            new DateOnly(2015, 3, 1), new DateOnly(1985, 7, 7), Opening, new DateOnly(2026, 6, 1)));

        Assert.Equal(new DateOnly(2026, 3, 1), accrual.Anniversary);
        Assert.Equal(11, accrual.SeniorityYears);
    }

    [Fact]
    public void Entitlement_AnniversaryOnOpeningDate_IsAccrued() =>
        Assert.Single(EntitlementCalculator.Accruals(
            new DateOnly(2023, 1, 1), new DateOnly(1990, 1, 1), Opening, Opening));

    [Fact]
    public void Anniversary_HiredFeb29_NonLeapYear_IsFeb28()
    {
        Assert.Equal(new DateOnly(2027, 2, 28), EntitlementCalculator.Anniversary(new DateOnly(2024, 2, 29), 3));
        Assert.Equal(new DateOnly(2028, 2, 29), EntitlementCalculator.Anniversary(new DateOnly(2024, 2, 29), 4));
    }

    [Fact]
    public void Entitlement_UnusedDays_CarryOverUnlimited()
    {
        // Two anniversaries after opening both stay in the total; nothing expires.
        var accruals = EntitlementCalculator.Accruals(
            new DateOnly(2020, 2, 1), new DateOnly(1990, 1, 1), Opening, new DateOnly(2027, 6, 1));

        Assert.Equal([new DateOnly(2026, 2, 1), new DateOnly(2027, 2, 1)], accruals.Select(a => a.Anniversary));
        Assert.Equal(20 + 20, accruals.Sum(a => a.Days));
    }

    [Theory]
    [InlineData("1990-05-10", "2026-05-09", 35)]
    [InlineData("1990-05-10", "2026-05-10", 36)]
    [InlineData("2008-02-29", "2026-02-27", 17)]
    [InlineData("2008-02-29", "2026-02-28", 18)] // Feb 29 birthday counts on Feb 28 in non-leap years, as hire anniversaries do (AC-14)
    public void AgeOn_CountsCompletedYears(string birth, string on, int expected) =>
        Assert.Equal(expected, EntitlementCalculator.AgeOn(DateOnly.Parse(birth, CultureInfo.InvariantCulture), DateOnly.Parse(on, CultureInfo.InvariantCulture)));
}
