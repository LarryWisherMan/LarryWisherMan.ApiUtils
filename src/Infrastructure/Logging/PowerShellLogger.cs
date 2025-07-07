namespace LarryWisherMan.ApiUtils.Infrastructure.Logging
{
    using System;
    using System.IO;
    using System.Management.Automation;

    /// <summary>
    /// Log levels for the lightweight logger
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6
    }

    /// <summary>
    /// Lightweight logger interface
    /// </summary>
    public interface IApiLogger
    {
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCritical(string message, params object[] args);
        bool IsEnabled(LogLevel logLevel);
    }

    /// <summary>
    /// PowerShell-aware lightweight logger
    /// </summary>
    public class PowerShellLogger : IApiLogger
    {
        private readonly string _categoryName;
        private readonly PSCmdlet _cmdlet;
        private static LogLevel _minLogLevel = LogLevel.Information;

        public PowerShellLogger(string categoryName, PSCmdlet cmdlet = null)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _cmdlet = cmdlet;
        }

        public static void SetMinimumLogLevel(LogLevel logLevel)
        {
            _minLogLevel = logLevel;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minLogLevel;
        }

        public void LogTrace(string message, params object[] args)
        {
            Log(LogLevel.Trace, message, null, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, null, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            Log(LogLevel.Information, message, null, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, null, args);
        }

        public void LogError(string message, params object[] args)
        {
            Log(LogLevel.Error, message, null, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            Log(LogLevel.Error, message, exception, args);
        }

        public void LogCritical(string message, params object[] args)
        {
            Log(LogLevel.Critical, message, null, args);
        }

        private void Log(LogLevel logLevel, string message, Exception exception = null, params object[] args)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var formattedMessage = args?.Length > 0 ? string.Format(message, args) : message;
                var fullMessage = $"[{_categoryName}] {formattedMessage}";

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
                                _cmdlet.WriteError(new ErrorRecord(exception, "LogError", ErrorCategory.NotSpecified, null));
                            }
                            else
                            {
                                _cmdlet.WriteError(new ErrorRecord(new Exception(fullMessage), "LogError", ErrorCategory.NotSpecified, null));
                            }
                            break;
                    }
                }
                else
                {
                    // Fallback to console if no cmdlet context
                    Console.WriteLine($"[{logLevel}] {fullMessage}");
                    if (exception != null)
                    {
                        Console.WriteLine(exception.ToString());
                    }
                }
            }
            catch
            {
                // Swallow logging errors to prevent breaking the main application
            }
        }
    }

    /// <summary>
    /// Simple logger factory
    /// </summary>
    public static class LoggerFactory
    {
        public static IApiLogger CreateLogger(string categoryName, PSCmdlet cmdlet = null)
        {
            return new PowerShellLogger(categoryName, cmdlet);
        }

        public static IApiLogger CreateLogger<T>(PSCmdlet cmdlet = null)
        {
            return new PowerShellLogger(typeof(T).Name, cmdlet);
        }

        public static void SetLogLevel(LogLevel logLevel)
        {
            PowerShellLogger.SetMinimumLogLevel(logLevel);
        }
    }

    /// <summary>
    /// Extension methods for easier logging
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogHttpRequest(this IApiLogger logger, string method, string url, object headers = null, object body = null)
        {
            logger.LogInformation("HTTP {0} {1}", method, url);

            if (headers != null)
            {
                logger.LogDebug("Request Headers: {0}", SerializeObject(headers));
            }

            if (body != null)
            {
                var bodyStr = body is string s ? s : SerializeObject(body);
                if (bodyStr.Length > 1000)
                {
                    bodyStr = bodyStr.Substring(0, 1000) + "... (truncated)";
                }
                logger.LogDebug("Request Body: {0}", bodyStr);
            }
        }

        public static void LogHttpResponse(this IApiLogger logger, int statusCode, string statusDescription, string content = null, object headers = null)
        {
            if (statusCode >= 400)
            {
                logger.LogWarning("HTTP Response {0} {1}", statusCode, statusDescription);
            }
            else
            {
                logger.LogInformation("HTTP Response {0} {1}", statusCode, statusDescription);
            }

            if (headers != null)
            {
                logger.LogDebug("Response Headers: {0}", SerializeObject(headers));
            }

            if (!string.IsNullOrEmpty(content))
            {
                var contentStr = content.Length > 1000 ? content.Substring(0, 1000) + "... (truncated)" : content;
                logger.LogDebug("Response Content: {0}", contentStr);
            }
        }

        public static void LogSessionOperation(this IApiLogger logger, string operation, string sessionName, object details = null)
        {
            logger.LogInformation("Session {0}: {1}", operation, sessionName);

            if (details != null)
            {
                logger.LogDebug("Session Details: {0}", SerializeObject(details));
            }
        }

        private static string SerializeObject(object obj)
        {
            if (obj == null) return "null";
            if (obj is string str) return str;

            try
            {
                // Simple serialization for basic types
                return obj.ToString();
            }
            catch
            {
                return "[Object]";
            }
        }
    }
}
