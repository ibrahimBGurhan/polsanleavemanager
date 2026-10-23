namespace PolsanLeave.SharedKernel;

/// <summary>
/// The only source of "now" in production code (FD-5). Leave dates are calendar days in
/// Europe/Istanbul (docs/conventions.md, Data rules).
/// </summary>
public interface IBusinessClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's calendar date in Europe/Istanbul.</summary>
    DateOnly Today { get; }
}
