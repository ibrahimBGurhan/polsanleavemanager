using System.Diagnostics.CodeAnalysis;

namespace PolsanLeave.SharedKernel;

/// <summary>Outcome of an operation that can fail for an expected business reason (docs/conventions.md, Error handling).</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    internal Result(T? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result is a failure ({Error.Code}); it has no value.");

#pragma warning disable CA2225 // Named alternates are Result.Success / Result.Failure.
    public static implicit operator Result<T>(T value) => Result.Success(value);

    public static implicit operator Result<T>(Error error) => Result.Failure<T>(error);
#pragma warning restore CA2225
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Failure<T>(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));
}
