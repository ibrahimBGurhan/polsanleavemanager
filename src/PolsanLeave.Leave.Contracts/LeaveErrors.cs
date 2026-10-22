using PolsanLeave.SharedKernel;

namespace PolsanLeave.Leave.Contracts;

/// <summary>Expected failures of spec 0001 (R-6). Codes map to Turkish messages in the UI resources.</summary>
public static class LeaveErrors
{
    public const int MaxDescriptionLength = 500;

    public const int MaxDaysAhead = 365;

    public static readonly Error ProfileMissing = new("Leave.ProfileMissing");
    public static readonly Error NoApprover = new("Leave.NoApprover");
    public static readonly Error StartInPast = new("Leave.StartInPast", "StartDate");
    public static readonly Error StartTooFar = new("Leave.StartTooFar", "StartDate");
    public static readonly Error EndBeforeStart = new("Leave.EndBeforeStart", "EndDate");
    public static readonly Error NoWorkingDays = new("Leave.NoWorkingDays", "EndDate");
    public static readonly Error InsufficientBalance = new("Leave.InsufficientBalance", "EndDate");
    public static readonly Error Overlap = new("Leave.Overlap", "StartDate");
    public static readonly Error DescriptionTooLong = new("Leave.DescriptionTooLong", "Description");
}
