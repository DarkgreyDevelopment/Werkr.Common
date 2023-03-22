using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Werkr.Common.Logging {
    public static class WerkrLoggingExtensions {

        /// <summary>
        /// Reads the ${AppContext.BaseDirectory}/log4net.config file and configures a Log4NetProvider.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="serviceName"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddLog4Net(
            this WebApplicationBuilder builder,
            string serviceName,
            string configPath = null
        ) {
            configPath ??= Path.Combine( AppContext.BaseDirectory, "log4net.config" );
            ConfigureFromLog4NetXmlConfigFile( new FileInfo( configPath ) );
            _ = builder.Logging.AddProvider( new Log4NetProvider( new WerkrILogger( serviceName ) ) );

            return builder;
        }


        /// <summary>
        /// Ensures the <paramref name="configFile"/> exists then uses
        /// <seealso cref="XmlConfigurator.Configure(ILoggerRepository,FileInfo)"/> to configure the
        /// <see cref="log4net.Repository.ILoggerRepository"/>.
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

        /// <summary>
        /// Enables OpenTelemetry tracing if the "OTLP:EnableTelemetry" logsection is true.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddOpenTelemetryTracing(
            this WebApplicationBuilder builder,
            string serviceName
        ) {
            IConfigurationSection logSection = builder.Configuration.GetSection( "Logging" );
            // Add Open Telemetry Tracing
            if (logSection["OTLP:EnableTelemetry"]?.ToLower( ) == "true") {
                // Configure OpenTelemetry tracing.
                _ = builder.Services.ConfigureOpenTelemetryTracerProvider(
                    ( isp, tpb ) => {
                        _ = tpb
                            .AddSource( serviceName )
                            .AddHttpClientInstrumentation( )
                            .AddAspNetCoreInstrumentation( )
                            .AddGrpcClientInstrumentation( )
                            .SetResourceBuilder( ResourceBuilder.CreateDefault( ).AddService( serviceName, null, GetAssemblyVersion( ) ) )
                            .AddOtlpExporter( a => a.Endpoint = new Uri( logSection["OTLP:CollectorAddress"] ) );
                    }
                );
                _ = builder.Logging.AddOpenTelemetry( );
            }
            return builder;
        }

        /// <summary>
        /// Retrieves the entry assembly file version if its available.
        /// </summary>
        private static string GetAssemblyVersion( ) => (null == Assembly.GetEntryAssembly( ))
            ? null
            : System.Diagnostics.FileVersionInfo.GetVersionInfo( Assembly.GetEntryAssembly( )!.Location ).FileVersion;

        /// <summary>
        /// Enables OpenTelemtry metrics if the "OTLP:EnableTelemetry" logsection is true.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddOpenTelemetryMetrics(
            this WebApplicationBuilder builder,
            string serviceName
        ) {
            IConfigurationSection logSection = builder.Configuration.GetSection( "Logging" );
            // Add Open Telemetry Tracing
            if (logSection["OTLP:EnableTelemetry"]?.ToLower( ) == "true") {
                // Configure OpenTelemetry metrics.
                _ = builder.Services.ConfigureOpenTelemetryMeterProvider(
                    ( isp, tpb ) => {
                        _ = tpb
                            .AddMeter( serviceName )
                            .AddOtlpExporter( a => a.Endpoint = new Uri( logSection["OTLP:CollectorAddress"] ) );
                    }
                );
                _ = builder.Logging.AddOpenTelemetry( );
            }
            return builder;
        }
    }
}
