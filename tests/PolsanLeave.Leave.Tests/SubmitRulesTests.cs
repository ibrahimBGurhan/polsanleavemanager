using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Domain;

namespace PolsanLeave.Leave.Tests;

/// <summary>Spec 0001, R-6 (AC-23 … AC-31). "Today" is Thursday 2026-10-22.</summary>
public class SubmitRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private static LeaveRequest Existing(DateOnly start, DateOnly end, LeaveRequestStatus status) =>
        LeaveRequests.WithStatus(LeaveRequest.Submit(Guid.NewGuid(), Guid.NewGuid(), start, end, 1, null, Now), status);

    [Fact]
    public void Submit_StartToday_Accepted() =>
        Assert.Null(SubmitRules.ValidateInput(Today, Today, null, Today));

    [Fact]
    public void Submit_StartYesterday_RejectedStartInPast() =>
        Assert.Equal(LeaveErrors.StartInPast, SubmitRules.ValidateInput(Today.AddDays(-1), Today, null, Today));

    [Fact]
    public void Submit_EndBeforeStart_Rejected() =>
        Assert.Equal(LeaveErrors.EndBeforeStart, SubmitRules.ValidateInput(Today.AddDays(5), Today.AddDays(4), null, Today));

    [Fact]
    public void Submit_StartTodayPlus365_Accepted() =>
        Assert.Null(SubmitRules.ValidateInput(Today.AddDays(365), Today.AddDays(365), null, Today));

    [Fact]
    public void Submit_StartTodayPlus366_Rejected() =>
        Assert.Equal(LeaveErrors.StartTooFar, SubmitRules.ValidateInput(Today.AddDays(366), Today.AddDays(366), null, Today));

    [Fact]
    public void Submit_Description500_Accepted() =>
        Assert.Null(SubmitRules.ValidateInput(Today, Today, new string('a', 500), Today));

    [Fact]
    public void Submit_Description501_Rejected() =>
        Assert.Equal(LeaveErrors.DescriptionTooLong, SubmitRules.ValidateInput(Today, Today, new string('a', 501), Today));

    [Fact]
    public void Submit_DescriptionEmpty_Accepted()
    {
        Assert.Null(SubmitRules.ValidateInput(Today, Today, "", Today));
        Assert.Null(SubmitRules.ValidateInput(Today, Today, null, Today));
    }

    [Fact]
    public void Submit_OnlyWeekendAndHolidays_RejectedNoWorkingDays() =>
        Assert.Equal(LeaveErrors.NoWorkingDays, SubmitRules.ValidateAgainstState(new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 25), 0, [], 10));

    [Fact]
    public void Submit_DaysEqualAvailable_Accepted() =>
        Assert.Null(SubmitRules.ValidateAgainstState(Today, Today.AddDays(6), 5, [], 5));

    [Fact]
    public void Submit_AvailablePlusOne_RejectedInsufficient() =>
        Assert.Equal(LeaveErrors.InsufficientBalance, SubmitRules.ValidateAgainstState(Today, Today.AddDays(7), 6, [], 5));

    [Theory]
    [InlineData(LeaveRequestStatus.Pending)]
    [InlineData(LeaveRequestStatus.Approved)]
    public void Submit_OverlapsPendingOrApproved_Rejected(LeaveRequestStatus status)
    {
        // Existing 2–6 Nov; new 6–10 Nov shares a single day.
        var existing = Existing(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), status);

        Assert.Equal(LeaveErrors.Overlap, SubmitRules.ValidateAgainstState(new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 10), 3, [existing], 20));
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void Submit_OverlapsRejectedOrCancelled_Accepted(LeaveRequestStatus status)
    {
        var existing = Existing(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), status);

        Assert.Null(SubmitRules.ValidateAgainstState(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), 5, [existing], 20));
    }

    [Fact]
    public void Submit_AdjacentToExisting_NotOverlap()
    {
        var existing = Existing(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), LeaveRequestStatus.Pending);

        Assert.Null(SubmitRules.ValidateAgainstState(new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 9), 1, [existing], 20));
    }
}
