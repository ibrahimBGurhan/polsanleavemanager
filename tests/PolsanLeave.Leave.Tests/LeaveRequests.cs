using System.Reflection;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Domain;

namespace PolsanLeave.Leave.Tests;

/// <summary>
/// Test data: requests in states this slice cannot reach yet (approval/rejection/cancellation arrive in
/// later specs — spec 0001, Constraints note). Production code has no API for it on purpose.
/// </summary>
internal static class LeaveRequests
{
    private static readonly PropertyInfo StatusProperty = typeof(LeaveRequest).GetProperty(nameof(LeaveRequest.Status))!;

    public static LeaveRequest WithStatus(LeaveRequest request, LeaveRequestStatus status)
    {
        StatusProperty.SetValue(request, status);
        return request;
    }
}
