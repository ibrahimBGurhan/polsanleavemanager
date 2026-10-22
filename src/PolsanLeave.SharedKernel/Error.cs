namespace PolsanLeave.SharedKernel;

/// <summary>
/// An expected business failure. <see cref="Code"/> is stable (<c>Module.Reason</c>, docs/conventions.md)
/// and maps to a Turkish UI message; <see cref="Field"/> names the input the error belongs to, if any.
/// </summary>
public sealed record Error(string Code, string? Field = null);
