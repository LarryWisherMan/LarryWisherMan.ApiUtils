using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace LarryWisherMan.ApiUtils.Infrastructure.Logging
{
    /// <summary>
    /// PowerShell-aware logger that writes to both ILogger and PowerShell streams
    /// </summary>
    public class PowerShellLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly PSCmdlet _cmdlet;
        private readonly ILogger _innerLogger;

        public PowerShellLogger(string categoryName, PSCmdlet cmdlet, ILogger innerLogger = null)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _cmdlet = cmdlet;
            _innerLogger = innerLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => _innerLogger?.BeginScope(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var fullMessage = $"[{_categoryName}] {message}";

            // Log to inner logger if available
            _innerLogger?.Log(logLevel, eventId, state, exception, formatter);

            // Log to PowerShell streams based on log level
            if (_cmdlet != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        _cmdlet.WriteDebug(fullMessage);
                        break;
                    case LogLevel.Information:
                        _cmdlet.WriteVerbose(fullMessage);
                        break;
                    case LogLevel.Warning:
                        _cmdlet.WriteWarning(fullMessage);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        if (exception != null)
                        {
                            _cmdlet.WriteError(new ErrorRecord(exception, eventId.ToString(), ErrorCategory.NotSpecified, null));
                        }
                        else
                        {
                            _cmdlet.WriteError(new ErrorRecord(new Exception(fullMessage), eventId.ToString(), ErrorCategory.NotSpecified, null));
                        }
                        break;
                }
            }

            // Also write to console if no cmdlet context
            if (_cmdlet == null)
            {
                Console.WriteLine($"[{logLevel}] {fullMessage}");
                if (exception != null)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Logger factory for creating PowerShell-aware loggers
    /// </summary>
    public class PowerShellLoggerFactory
    {
        private static ILoggerFactory _loggerFactory;
        private static readonly object _lock = new object();

        public static ILoggerFactory Instance
        {
            get
            {
                if (_loggerFactory == null)
                {
                    lock (_lock)
                    {
                        if (_loggerFactory == null)
                        {
                            _loggerFactory = LoggerFactory.Create(builder =>
                            {
                                builder
                                    .SetMinimumLevel(LogLevel.Debug)
                                    .AddConsole()
                                    .AddDebug();

                                // Add file logging if desired
                                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ApiUtils", "logs");
                                if (!Directory.Exists(logPath))
                                {
                                    Directory.CreateDirectory(logPath);
                                }
                            });
                        }
                    }
                }
                return _loggerFactory;
            }
        }

        public static ILogger<T> CreateLogger<T>(PSCmdlet cmdlet = null)
        {
            var innerLogger = Instance.CreateLogger<T>();
            return new PowerShellLogger<T>(cmdlet, innerLogger);
        }

        public static ILogger CreateLogger(string categoryName, PSCmdlet cmdlet = null)
        {
            var innerLogger = Instance.CreateLogger(categoryName);
            return new PowerShellLogger(categoryName, cmdlet, innerLogger);
        }

        public static void SetLogLevel(LogLevel minLevel)
        {
            // Recreate factory with new log level
            lock (_lock)
            {
                _loggerFactory?.Dispose();
                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .SetMinimumLevel(minLevel)
                        .AddConsole()
                        .AddDebug();
                });
            }
        }
    }

    /// <summary>
    /// Generic version of PowerShellLogger
    /// </summary>
    public class PowerShellLogger<T> : ILogger<T>
    {
        private readonly PowerShellLogger _logger;

        public PowerShellLogger(PSCmdlet cmdlet, ILogger<T> innerLogger = null)
        {
            _logger = new PowerShellLogger(typeof(T).Name, cmdlet, innerLogger);
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    /// <summary>
    /// Extension methods for easier logging
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogHttpRequest(this ILogger logger, string method, string url, object headers = null, object body = null)
        {
            logger.LogInformation("HTTP {Method} {Url}", method, url);

            if (headers != null)
            {
                logger.LogDebug("Request Headers: {Headers}", System.Text.Json.JsonSerializer.Serialize(headers));
            }

            if (body != null)
            {
                var bodyStr = body is string s ? s : System.Text.Json.JsonSerializer.Serialize(body);
                if (bodyStr.Length > 1000)
                {
                    bodyStr = bodyStr.Substring(0, 1000) + "... (truncated)";
                }
                logger.LogDebug("Request Body: {Body}", bodyStr);
            }
        }

        public static void LogHttpResponse(this ILogger logger, int statusCode, string statusDescription, string content = null, object headers = null)
        {
            var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(logLevel, "HTTP Response {StatusCode} {StatusDescription}", statusCode, statusDescription);

            if (headers != null)
            {
                logger.LogDebug("Response Headers: {Headers}", System.Text.Json.JsonSerializer.Serialize(headers));
            }

            if (!string.IsNullOrEmpty(content))
            {
                var contentStr = content.Length > 1000 ? content.Substring(0, 1000) + "... (truncated)" : content;
                logger.LogDebug("Response Content: {Content}", contentStr);
            }
        }

        public static void LogSessionOperation(this ILogger logger, string operation, string sessionName, object details = null)
        {
            logger.LogInformation("Session {Operation}: {SessionName}", operation, sessionName);

            if (details != null)
            {
                logger.LogDebug("Session Details: {Details}", System.Text.Json.JsonSerializer.Serialize(details));
            }
        }
    }
}
