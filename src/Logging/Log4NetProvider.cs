using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Werkr.Common.Logging {
    /// <summary>
    /// Implementation of the <see cref="ILoggerProvider"/> interface that uses log4net.
    /// </summary>
    public class Log4NetProvider : ILoggerProvider {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetProvider"/> class.
        /// </summary>
        public Log4NetProvider( ILogger logger ) {
            _logger = logger;
        }

        /// <summary>
        /// Creates a logger for the specified category name.
        /// </summary>
        public ILogger CreateLogger( string categoryName ) {
            return _loggers.GetOrAdd( categoryName, CreateLoggerImplementation );
        }

        public void Dispose( ) {
            GC.SuppressFinalize( this );
            _loggers.Clear( );
        }

        private ILogger CreateLoggerImplementation( string name ) => _logger;
    }
}
