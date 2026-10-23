using PolsanLeave.Balance.Contracts;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Domain;

namespace PolsanLeave.Leave.Tests;

/// <summary>Spec 0001, R-3 (AC-5, AC-15, AC-28, AC-34).</summary>
public class BalanceSummaryCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static Entitlement Opening(int days) =>
        new([new EntitlementEntry(new DateOnly(2026, 1, 1), days, EntitlementSource.OpeningBalance, "Açılış bakiyesi")]);

    private static LeaveRequest Request(int workingDays, LeaveRequestStatus status, int startDay = 2) =>
        LeaveRequests.WithStatus(
            LeaveRequest.Submit(EmployeeId, Guid.NewGuid(), new DateOnly(2026, 11, startDay), new DateOnly(2026, 11, startDay), workingDays, null, Now),
            status);

    [Fact]
    public void Summary_OpeningBalanceOnly_NoRequests_Returns10_0_10()
    {
        var summary = BalanceSummaryCalculator.Calculate(Opening(10), []);

        Assert.Equal((10, 0, 10), (summary.Remaining, summary.Pending, summary.Available));
    }

    [Fact]
    public void Summary_ApprovedReducesRemaining_PendingReducesAvailable()
    {
        var summary = BalanceSummaryCalculator.Calculate(Opening(20), [
            Request(5, LeaveRequestStatus.Approved, 2),
            Request(3, LeaveRequestStatus.Pending, 9),
            Request(4, LeaveRequestStatus.Rejected, 16),
            Request(2, LeaveRequestStatus.Cancelled, 23),
        ]);

        Assert.Equal((15, 3, 12), (summary.Remaining, summary.Pending, summary.Available));
    }

    [Fact]
    public void Submit_PendingCountsAgainstAvailable_Rejected()
    {
        // Kalan 10, Bekleyen 6 → Kullanılabilir 4: a 5-day request is rejected (AC-28).
        var existing = Request(6, LeaveRequestStatus.Pending);
        var available = BalanceSummaryCalculator.Calculate(Opening(10), [existing]).Available;

        Assert.Equal(4, available);
        Assert.Equal(LeaveErrors.InsufficientBalance,
            SubmitRules.ValidateAgainstState(new DateOnly(2026, 12, 7), new DateOnly(2026, 12, 11), 5, [existing], available));
    }

    [Fact]
    public void Summary_EveryMovementHasSource()
    {
        var entitlement = new Entitlement([
            new EntitlementEntry(new DateOnly(2026, 1, 1), 10, EntitlementSource.OpeningBalance, "Açılış bakiyesi"),
            new EntitlementEntry(new DateOnly(2026, 6, 15), 20, EntitlementSource.Accrual, "6. yıl hak edişi"),
        ]);
        var approved = Request(5, LeaveRequestStatus.Approved);

        var summary = BalanceSummaryCalculator.Calculate(entitlement, [approved]);

        Assert.All(summary.Movements, m => Assert.False(string.IsNullOrWhiteSpace(m.Reference)));
        Assert.Contains(summary.Movements, m => m.Source == BalanceMovementSource.ApprovedRequest && m.Reference == approved.Id.ToString() && m.Days == -5);
        Assert.Equal(summary.Remaining, summary.Movements.Sum(m => m.Days));
    }
}
