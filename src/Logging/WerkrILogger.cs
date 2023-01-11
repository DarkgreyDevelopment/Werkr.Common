using System;
using log4net;
using Microsoft.Extensions.Logging;

namespace Werkr.Common.Logging {
    public class WerkrILogger : ILogger {
        /// <summary>
        /// Initializes a new instance of the <see cref="WerkrILogger"/> class using the specified service name and log4net for logging.
        /// </summary>
        /// <param name="serviceName">The service name to use for logging.</param>
        public WerkrILogger( string serviceName ) {
            Log4NetLog = LogManager.GetLogger( serviceName );
        }

        internal readonly ILog Log4NetLog;

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>( TState state ) {
            return state != null ? default : (IDisposable)null;
        }

        /// <summary>
        /// Checks if logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check.</param>
        /// <returns>True if logging is enabled, false otherwise.</returns>
        public bool IsEnabled( LogLevel logLevel ) {
            switch (logLevel) {
                case LogLevel.Critical:
                    return IsFatalEnabled( );
                case LogLevel.Error:
                    return IsErrorEnabled( );
                case LogLevel.Warning:
                    return IsWarnEnabled( );
                case LogLevel.Information:
                    return IsInfoEnabled( );
                case LogLevel.Debug:
                    return IsDebugEnabled( );
                case LogLevel.Trace:
                    return IsDebugEnabled( );
                case LogLevel.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException( nameof( logLevel ) );
            }
        }

        private bool IsFatalEnabled( ) => Log4NetLog?.IsFatalEnabled ?? false;
        private bool IsErrorEnabled( ) => Log4NetLog?.IsErrorEnabled ?? false;
        private bool IsWarnEnabled( ) => Log4NetLog?.IsWarnEnabled ?? false;
        private bool IsInfoEnabled( ) => Log4NetLog?.IsInfoEnabled ?? false;
        private bool IsDebugEnabled( ) => Log4NetLog?.IsDebugEnabled ?? false;

        /// <summary>
        /// Writes a log message at the specified log level.
        /// </summary>
        /// <typeparam name="TState">The type of the state object.</typeparam>
        /// <param name="logLevel">The log level of the message.</param>
        /// <param name="eventId">The event ID for the log message.</param>
        /// <param name="state">The state object for the log message.</param>
        /// <param name="exception">An optional exception object for the log message.</param>
        /// <param name="formatter">A delegate for formatting the log message.</param>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter
        ) {
            if (Log4NetLog == null || IsEnabled( logLevel ) == false) { return; }

            string message = $"{formatter( state, exception )} {exception}";

            if (string.IsNullOrEmpty( message ) == false) {
                switch (logLevel) {
                    case LogLevel.Critical:
                        Log4NetLog.Fatal( message );
                        break;
                    case LogLevel.Error:
                        Log4NetLog.Error( message );
                        break;
                    case LogLevel.Warning:
                        Log4NetLog.Warn( message );
                        break;
                    case LogLevel.Information:
                        Log4NetLog.Info( message );
                        break;
                    case LogLevel.Debug:
                        Log4NetLog.Debug( message );
                        break;
                    case LogLevel.Trace:
                        Log4NetLog.Debug( message );
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( nameof( logLevel ) );
                };
            }
        }
    }
}
