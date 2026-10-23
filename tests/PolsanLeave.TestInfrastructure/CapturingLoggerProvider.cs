using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PolsanLeave.TestInfrastructure;

/// <summary>
/// Captures every log entry's formatted message — what a plain log line shows without scopes — so tests
/// can assert that support can find a correlation id in the logs (review 1, F-8).
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _entries = new();

    public IReadOnlyCollection<CapturedLog> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<CapturedLog> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                entries.Enqueue(new CapturedLog(category, logLevel, formatter(state, exception), exception));
            }
        }
    }
}

public sealed record CapturedLog(string Category, LogLevel Level, string Message, Exception? Exception);
