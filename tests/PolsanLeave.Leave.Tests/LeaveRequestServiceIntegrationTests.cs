using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Calendar.Contracts;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Data;
using PolsanLeave.Leave.Internal.Domain;
using PolsanLeave.SharedKernel;
using PolsanLeave.TestInfrastructure;

namespace PolsanLeave.Leave.Tests;

/// <summary>
/// The Leave service against real SQL Server: persistence, ownership and the per-employee lock.
/// Other modules are substituted through their Contracts (FD-1). "Today" is Thursday 2026-10-22.
/// </summary>
[Trait("Category", "Integration")]
public sealed class LeaveRequestServiceIntegrationTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly Guid ManagerId = Guid.NewGuid();

    private readonly TestDatabase _database = new();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly IWorkingDayService _workingDays = Substitute.For<IWorkingDayService>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();
    private readonly EmployeeProfile _ayse = new(Guid.NewGuid(), "1001", "Ayşe Yılmaz", new DateOnly(2020, 6, 15), new DateOnly(1992, 3, 3), ManagerId);
    private readonly EmployeeProfile _mehmet = new(Guid.NewGuid(), "1002", "Mehmet Kaya", new DateOnly(2019, 1, 7), new DateOnly(1988, 9, 9), ManagerId);
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _services = new ServiceCollection()
            .AddLeaveModule(_database.ConnectionString)
            .AddSingleton(_entitlements)
            .AddSingleton(_workingDays)
            .AddSingleton(_clock)
            .BuildServiceProvider();
        await _services.MigrateLeaveModuleAsync(TestContext.Current.CancellationToken);

        _clock.Today.Returns(Today);
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 9, 0, 0, TimeSpan.Zero));
        _entitlements.GetAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new Entitlement([new EntitlementEntry(new DateOnly(2026, 1, 1), 10, EntitlementSource.OpeningBalance, "Açılış bakiyesi")]));
        _workingDays.CountWorkingDaysAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(c => Result.Success(BusinessDays(c.ArgAt<DateOnly>(0), c.ArgAt<DateOnly>(1))));
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Submit_Valid_PersistsPendingAssignedToManager()
    {
        var result = await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6), "Aile ziyareti"));

        Assert.True(result.IsSuccess);
        Assert.Equal((LeaveRequestStatus.Pending, 5), (result.Value.Status, result.Value.WorkingDays));

        await using var scope = _services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<LeaveDbContext>().LeaveRequests
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal((_ayse.Id, ManagerId, LeaveRequestStatus.Pending, "Aile ziyareti"), (stored.EmployeeId, stored.ApproverId, stored.Status, stored.Description));
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyCurrentEmployeeRequests()
    {
        await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3), null));
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 10, 0, 0, TimeSpan.Zero));
        await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 9), null));
        await SubmitAsAsync(_mehmet, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2), null));

        var mine = await AsEmployee(_ayse).GetMineAsync(TestContext.Current.CancellationToken);

        Assert.Equal([new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 2)], mine.Value.Select(r => r.StartDate));
    }

    [Fact]
    public async Task Submit_Invalid_PersistsNothing()
    {
        var result = await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 20), null));

        Assert.Equal(LeaveErrors.InsufficientBalance, result.Error);
        await using var scope = _services.CreateAsyncScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<LeaveDbContext>().LeaveRequests.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Submit_OverlapsApprovedFromStore_Rejected()
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
            db.LeaveRequests.Add(LeaveRequests.WithStatus(
                LeaveRequest.Submit(_ayse.Id, ManagerId, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4), 3, null, _clock.UtcNow),
                LeaveRequestStatus.Approved));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 5), null));
        var summary = await AsEmployee(_ayse).GetSummaryAsync(TestContext.Current.CancellationToken);

        Assert.Equal(LeaveErrors.Overlap, result.Error);
        Assert.Equal((7, 0, 7), (summary.Value.Remaining, summary.Value.Pending, summary.Value.Available));
    }

    [Fact]
    public async Task Submit_NoManager_RejectedNoApprover()
    {
        var result = await SubmitAsAsync(_ayse with { ManagerId = null }, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2), null));

        Assert.Equal(LeaveErrors.NoApprover, result.Error);
        Assert.False((await AsEmployee(_ayse with { ManagerId = null }).GetSummaryAsync(TestContext.Current.CancellationToken)).Value.HasApprover);
        Assert.True((await AsEmployee(_ayse).GetSummaryAsync(TestContext.Current.CancellationToken)).Value.HasApprover);
    }

    [Fact]
    public async Task Submit_RangeWithoutHolidayCalendar_Rejected()
    {
        _workingDays.CountWorkingDaysAsync(new DateOnly(2027, 10, 18), new DateOnly(2028, 1, 3), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<int>(CalendarErrors.NotDefined));

        var result = await SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2027, 10, 18), new DateOnly(2028, 1, 3), null));

        // Bound to the end date field so the form can show it there (AC-32, AC-39; review 1, F-2).
        Assert.Equal(CalendarErrors.NotDefined.Code, result.Error?.Code);
        Assert.Equal("EndDate", result.Error?.Field);

        var preview = await AsEmployee(_ayse).PreviewAsync(new DateOnly(2027, 10, 18), new DateOnly(2028, 1, 3), TestContext.Current.CancellationToken);
        Assert.Equal((CalendarErrors.NotDefined.Code, "EndDate"), (preview.Error?.Code, preview.Error?.Field));
    }

    [Fact]
    public async Task Services_WithoutProfile_ReturnProfileMissing()
    {
        var service = AsEmployee(null);

        Assert.Equal(LeaveErrors.ProfileMissing, (await service.GetSummaryAsync(TestContext.Current.CancellationToken)).Error);
        Assert.Equal(LeaveErrors.ProfileMissing, (await service.GetMineAsync(TestContext.Current.CancellationToken)).Error);
        Assert.Equal(LeaveErrors.ProfileMissing, (await service.SubmitAsync(new SubmitLeaveRequest(Today, Today, null), TestContext.Current.CancellationToken)).Error);
    }

    [Fact]
    public async Task Preview_ReturnsDaysAndAvailableAfter()
    {
        var preview = await AsEmployee(_ayse).PreviewAsync(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4), TestContext.Current.CancellationToken);

        Assert.Equal(new LeavePreview(3, 7), preview.Value);
    }

    /// <summary>
    /// The balance read happens after the existing requests are read and before the insert; holding every
    /// submission there until two meet makes the race deterministic. The per-employee lock keeps them apart.
    /// </summary>
    private void ForceRaceAtBalanceRead()
    {
        var gate = new RendezvousGate(2, TimeSpan.FromSeconds(2));
        _entitlements.GetAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await gate.ArriveAsync();
                return new Entitlement([new EntitlementEntry(new DateOnly(2026, 1, 1), 10, EntitlementSource.OpeningBalance, "Açılış bakiyesi")]);
            });
    }

    [Fact]
    public async Task Submit_ConcurrentOverlapping_AtMostOneAccepted()
    {
        ForceRaceAtBalanceRead();

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3), null))));

        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => !r.IsSuccess), r => Assert.Equal(LeaveErrors.Overlap, r.Error));
    }

    [Fact]
    public async Task Submit_ConcurrentJointlyExceedingBalance_AtMostOneAccepted()
    {
        // Available 10; two non-overlapping 6-day requests cannot both fit.
        ForceRaceAtBalanceRead();

        var results = await Task.WhenAll(
            SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 9), null)),
            SubmitAsAsync(_ayse, new SubmitLeaveRequest(new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 23), null)));

        Assert.Single(results, r => r.IsSuccess);
        Assert.Single(results, r => r.Error == LeaveErrors.InsufficientBalance);
    }

    private async Task<Result<LeaveRequestItem>> SubmitAsAsync(EmployeeProfile employee, SubmitLeaveRequest request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await CreateService(scope, employee).SubmitAsync(request, TestContext.Current.CancellationToken);
    }

    private Internal.LeaveRequestService AsEmployee(EmployeeProfile? employee) => CreateService(_services.CreateScope(), employee);

    private static Internal.LeaveRequestService CreateService(IServiceScope scope, EmployeeProfile? employee)
    {
        var current = Substitute.For<ICurrentEmployee>();
        current.GetAsync(Arg.Any<CancellationToken>()).Returns(employee);
        return ActivatorUtilities.CreateInstance<Internal.LeaveRequestService>(scope.ServiceProvider, current);
    }

    private static int BusinessDays(DateOnly start, DateOnly end)
    {
        var days = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            days += d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0 : 1;
        }

        return days;
    }
}
