namespace PolsanLeave.Balance.Internal.Domain;

/// <summary>BR-1 and BR-2 (4857 sayılı İş Kanunu md. 53), computed on read (plan 0001, Risk 2).</summary>
internal static class EntitlementCalculator
{
    public sealed record Accrual(DateOnly Anniversary, int SeniorityYears, int AgeAtAnniversary, int Days);

    /// <summary>
    /// Accruals for every hire anniversary on or after <paramref name="openingDate"/> and on or before
    /// <paramref name="asOf"/>. Earlier anniversaries are covered by the opening balance.
    /// </summary>
    public static IReadOnlyList<Accrual> Accruals(DateOnly hireDate, DateOnly birthDate, DateOnly openingDate, DateOnly asOf)
    {
        var accruals = new List<Accrual>();
        for (var years = 1; ; years++)
        {
            var anniversary = Anniversary(hireDate, years);
            if (anniversary > asOf)
            {
                return accruals;
            }

            if (anniversary >= openingDate)
            {
                var age = AgeOn(birthDate, anniversary);
                accruals.Add(new Accrual(anniversary, years, age, AnnualDays(years, age)));
            }
        }
    }

    /// <summary>The n-th hire anniversary; a 29 February hire date falls on 28 February in non-leap years.</summary>
    public static DateOnly Anniversary(DateOnly hireDate, int years) => hireDate.AddYears(years);

    /// <summary>BR-1: 14 days up to and including 5 years, 20 days below 15 years, 26 days from 15 years;
    /// at least 20 days at age 18 and under or 50 and over.</summary>
    public static int AnnualDays(int seniorityYears, int age)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(seniorityYears, 1);

        var days = seniorityYears switch
        {
            <= 5 => 14,
            < 15 => 20,
            _ => 26,
        };

        return age <= 18 || age >= 50 ? Math.Max(days, 20) : days;
    }

    public static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        return date < birthDate.AddYears(age) ? age - 1 : age;
    }
}
