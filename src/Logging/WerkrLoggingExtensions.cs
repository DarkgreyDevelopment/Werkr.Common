using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Werkr.Common.Logging {
    public static class WerkrLoggingExtensions {
        public static WebApplicationBuilder AddLog4NetAndOpenTelemetry(
            this WebApplicationBuilder builder,
            string serviceName,
            string configPath = null
        ) {
            if (configPath == null) { configPath = Path.Combine( AppContext.BaseDirectory, "log4net.config" ); }
            ConfigureFromLog4NetXmlConfigFile( new FileInfo( configPath ) );

            IConfigurationSection logSection = builder.Configuration.GetSection( "Logging" );
            // Add Open Telemetry Tracing
            if (logSection["OTLP:EnableTelemetry"]?.ToLower( ) == "true") {
                _ = builder.Services.AddOpenTelemetryTracing(
                    tpb => {
                        _ = tpb
                            .AddHttpClientInstrumentation( )
                            .AddAspNetCoreInstrumentation( )
                            .AddGrpcClientInstrumentation( )
                            .AddSource( serviceName )
                            .SetResourceBuilder( ResourceBuilder.CreateDefault( ).AddService( serviceName ) )
                            .AddOtlpExporter( a =>
                                a.Endpoint = new Uri( logSection["OTLP:CollectorAddress"] )
                            );
                    }
                );
            }

            _ = builder.Logging.AddProvider( new Log4NetProvider( new WerkrILogger( serviceName ) ) );
            _ = builder.Logging.AddOpenTelemetry( );

            return builder;
        }

        /// <summary>
        /// Ensures the <paramref name="configFile"/> exists then uses
        /// <seealso cref="XmlConfigurator.Configure(ILoggerRepository,FileInfo)"/> to configure the
        /// <see cref="LoggerRepository"/>.
        /// </summary>
        /// <param name="configFile"></param>
        private static void ConfigureFromLog4NetXmlConfigFile( FileInfo configFile ) {
            if (File.Exists( configFile.FullName ) == false) {
                throw new FileNotFoundException(
                    $"Log4Net ConfigurationFile '{configFile.FullName}' doesn't exist."
                );
            }

            _ = XmlConfigurator.Configure(
                LogManager.GetRepository( Assembly.GetEntryAssembly( ) ),
                configFile
            );
        }
    }
}
