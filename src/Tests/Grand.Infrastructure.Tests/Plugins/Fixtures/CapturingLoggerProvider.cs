using Microsoft.Extensions.Logging;

namespace Grand.Infrastructure.Tests.Plugins.Fixtures;

/// <summary>
///     Keeps every entry logged through it, for tests that check what a loader logged
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<(string Category, LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(string Category, LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Logger(CapturingLoggerProvider provider, string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            lock (provider._entries)
            {
                provider._entries.Add((category, logLevel, formatter(state, exception)));
            }
        }
    }
}
