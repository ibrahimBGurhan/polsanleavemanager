namespace PolsanLeave.Balance.Internal.Domain;

/// <summary>
/// A reasoned manual credit or debit of annual-leave days (BR-8). Append-only: never edited or deleted;
/// a mistake is corrected by a reverse adjustment (docs/domain.md, invariants).
/// </summary>
internal sealed class BalanceAdjustment
{
    public const int MaxReasonLength = 500;

    public const string OpeningBalanceReason = "Açılış bakiyesi";

    private BalanceAdjustment()
    {
    }

    public BalanceAdjustment(Guid id, Guid employeeId, DateOnly effectiveDate, int days, AdjustmentKind kind, string reason, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A balance adjustment requires a reason (BR-8).", nameof(reason));
        }

        if (reason.Length > MaxReasonLength)
        {
            throw new ArgumentException($"Reason exceeds {MaxReasonLength} characters.", nameof(reason));
        }

        Id = id;
        EmployeeId = employeeId;
        EffectiveDate = effectiveDate;
        Days = days;
        Kind = kind;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateOnly EffectiveDate { get; private set; }

    public int Days { get; private set; }

    public AdjustmentKind Kind { get; private set; }

    public string Reason { get; private set; } = "";

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static BalanceAdjustment OpeningBalance(Guid id, Guid employeeId, DateOnly openingDate, int days, DateTimeOffset createdAtUtc) =>
        new(id, employeeId, openingDate, days, AdjustmentKind.OpeningBalance, OpeningBalanceReason, createdAtUtc);
}

internal enum AdjustmentKind
{
    OpeningBalance = 1,
    Manual = 2,
}

/// <summary>The date the system went live; anniversaries from this date on accrue automatically (spec 0001, R-3).</summary>
internal sealed class BalanceSettings
{
    public const int SingletonId = 1;

    private BalanceSettings()
    {
    }

    public BalanceSettings(DateOnly openingDate)
    {
        OpeningDate = openingDate;
    }

    public int Id { get; private set; } = SingletonId;

    public DateOnly OpeningDate { get; private set; }
}
