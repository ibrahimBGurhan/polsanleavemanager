using PolsanLeave.Leave.Contracts;

namespace PolsanLeave.Leave.Internal.Domain;

/// <summary>An annual-leave request (docs/domain.md, İzin talebi). Created Pending; approval arrives in spec 0002.</summary>
internal sealed class LeaveRequest
{
    private LeaveRequest()
    {
    }

    private LeaveRequest(Guid id, Guid employeeId, Guid approverId, DateOnly startDate, DateOnly endDate, int workingDays, string? description, DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmployeeId = employeeId;
        ApproverId = approverId;
        StartDate = startDate;
        EndDate = endDate;
        WorkingDays = workingDays;
        Description = description;
        Status = LeaveRequestStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>The employee's direct manager at submission time (BR-6).</summary>
    public Guid ApproverId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int WorkingDays { get; private set; }

    /// <summary>Personal data (KVKK): never logged.</summary>
    public string? Description { get; private set; }

    public LeaveRequestStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Pending or Approved — the requests that block overlaps (BR-5) and consume balance.</summary>
    public bool IsActive => Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved;

    public static LeaveRequest Submit(Guid employeeId, Guid approverId, DateOnly startDate, DateOnly endDate, int workingDays, string? description, DateTimeOffset nowUtc) =>
        new(Guid.CreateVersion7(nowUtc), employeeId, approverId, startDate, endDate, workingDays, string.IsNullOrWhiteSpace(description) ? null : description.Trim(), nowUtc);

    public bool Overlaps(DateOnly startDate, DateOnly endDate) => StartDate <= endDate && startDate <= EndDate;
}
