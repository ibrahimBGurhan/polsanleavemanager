using PolsanLeave.Leave.Contracts;
using PolsanLeave.SharedKernel;

namespace PolsanLeave.Leave.Internal.Domain;

/// <summary>The validation rules of spec 0001, R-6, as pure functions. Order: input shape, dates, then balance state.</summary>
internal static class SubmitRules
{
    /// <summary>Checks that need no stored state: description length and the date window.</summary>
    public static Error? ValidateInput(DateOnly startDate, DateOnly endDate, string? description, DateOnly today)
    {
        if (description is not null && description.Trim().Length > LeaveErrors.MaxDescriptionLength)
        {
            return LeaveErrors.DescriptionTooLong;
        }

        if (endDate < startDate)
        {
            return LeaveErrors.EndBeforeStart;
        }

        if (startDate < today)
        {
            return LeaveErrors.StartInPast;
        }

        if (startDate > today.AddDays(LeaveErrors.MaxDaysAhead))
        {
            return LeaveErrors.StartTooFar;
        }

        return null;
    }

    /// <summary>Checks against the employee's current requests and balance; run under the per-employee lock.</summary>
    public static Error? ValidateAgainstState(DateOnly startDate, DateOnly endDate, int workingDays, IEnumerable<LeaveRequest> existing, int available)
    {
        if (workingDays == 0)
        {
            return LeaveErrors.NoWorkingDays;
        }

        if (existing.Any(r => r.IsActive && r.Overlaps(startDate, endDate)))
        {
            return LeaveErrors.Overlap;
        }

        if (workingDays > available)
        {
            return LeaveErrors.InsufficientBalance;
        }

        return null;
    }
}
