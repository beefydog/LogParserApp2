using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AMSEmailLogParser.Tests
{
    public class TestLogger<T> : ILogger<T>
    {
        private readonly List<string> _logs = new List<string>();

        public IEnumerable<string> Logs => _logs;

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logs.Add(formatter(state, exception));
        }
    }
}
