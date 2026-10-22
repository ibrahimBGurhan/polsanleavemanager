using Microsoft.EntityFrameworkCore;
using PolsanLeave.Balance.Contracts;
using PolsanLeave.Calendar.Contracts;
using PolsanLeave.Identity.Contracts;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Data;
using PolsanLeave.Leave.Internal.Domain;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Leave.Internal;

internal sealed class LeaveRequestService(
    LeaveDbContext db,
    ICurrentEmployee currentEmployee,
    IEntitlementService entitlements,
    IWorkingDayService workingDays,
    IBusinessClock clock) : ILeaveRequestService
{
    private const int LockTimeoutMilliseconds = 10_000;

    public async Task<Result<BalanceSummary>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var employee = await currentEmployee.GetAsync(cancellationToken);
        if (employee is null)
        {
            return LeaveErrors.ProfileMissing;
        }

        var summary = await SummarizeAsync(employee.Id, cancellationToken);
        return summary with { HasApprover = employee.ManagerId is not null };
    }

    public async Task<Result<IReadOnlyList<LeaveRequestItem>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var employee = await currentEmployee.GetAsync(cancellationToken);
        if (employee is null)
        {
            return LeaveErrors.ProfileMissing;
        }

        var requests = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.EmployeeId == employee.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LeaveRequestItem>>(requests.Select(ToItem).ToList());
    }

    public async Task<Result<LeavePreview>> PreviewAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var employee = await currentEmployee.GetAsync(cancellationToken);
        if (employee is null)
        {
            return LeaveErrors.ProfileMissing;
        }

        if (SubmitRules.ValidateInput(startDate, endDate, null, clock.Today) is { } inputError)
        {
            return inputError;
        }

        var days = await workingDays.CountWorkingDaysAsync(startDate, endDate, cancellationToken);
        if (!days.IsSuccess)
        {
            return days.Error;
        }

        var summary = await SummarizeAsync(employee.Id, cancellationToken);
        return new LeavePreview(days.Value, summary.Available - days.Value);
    }

    public async Task<Result<LeaveRequestItem>> SubmitAsync(SubmitLeaveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var employee = await currentEmployee.GetAsync(cancellationToken);
        if (employee is null)
        {
            return LeaveErrors.ProfileMissing;
        }

        if (employee.ManagerId is not { } approverId)
        {
            return LeaveErrors.NoApprover;
        }

        var today = clock.Today;
        if (SubmitRules.ValidateInput(request.StartDate, request.EndDate, request.Description, today) is { } inputError)
        {
            return inputError;
        }

        var days = await workingDays.CountWorkingDaysAsync(request.StartDate, request.EndDate, cancellationToken);
        if (!days.IsSuccess)
        {
            return days.Error;
        }

        // BR-4/BR-5 under concurrency (AC-41): serialize this employee's submissions; the lock is
        // released with the transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AcquireEmployeeLockAsync(employee.Id, cancellationToken);

        var existing = await db.LeaveRequests.Where(r => r.EmployeeId == employee.Id).ToListAsync(cancellationToken);
        var entitlement = await entitlements.GetAsync(employee.Id, today, cancellationToken);
        var available = BalanceSummaryCalculator.Calculate(entitlement, existing).Available;

        if (SubmitRules.ValidateAgainstState(request.StartDate, request.EndDate, days.Value, existing, available) is { } stateError)
        {
            return stateError;
        }

        var leaveRequest = LeaveRequest.Submit(employee.Id, approverId, request.StartDate, request.EndDate, days.Value, request.Description, clock.UtcNow);
        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToItem(leaveRequest);
    }

    private async Task<BalanceSummary> SummarizeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var requests = await db.LeaveRequests.AsNoTracking().Where(r => r.EmployeeId == employeeId).ToListAsync(cancellationToken);
        var entitlement = await entitlements.GetAsync(employeeId, clock.Today, cancellationToken);
        return BalanceSummaryCalculator.Calculate(entitlement, requests);
    }

    private async Task AcquireEmployeeLockAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var resource = $"leave-employee-{employeeId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = {LockTimeoutMilliseconds};
            IF @result < 0 THROW 50001, 'Could not acquire the leave request lock.', 1;
            """,
            cancellationToken);
    }

    private static LeaveRequestItem ToItem(LeaveRequest r) =>
        new(r.Id, r.StartDate, r.EndDate, r.WorkingDays, r.Status, r.CreatedAtUtc);
}
