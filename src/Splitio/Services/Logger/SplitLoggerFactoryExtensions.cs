#if NET_LATEST
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    public static class SplitLoggerFactoryExtensions
    {
        private static ILoggerFactory _loggerFactory;

        public static ILoggerFactory AddSplitLogs(this ILoggerFactory factory)
        {
            _loggerFactory = factory;

            return factory;
        }

        public static ILoggerFactory GetLoggerFactory()
        {
            return _loggerFactory;
        }

        public static bool LoggerFactoryHasValue => _loggerFactory != null;

        /// <summary>
        /// Configures Split logs for ASP.NET Core applications.
        /// The logger factory will be captured when logging is first used in the application.
        /// </summary>
        public static ILoggingBuilder AddSplitLogs(this ILoggingBuilder builder)
        {
            // Replace the ILoggerFactory registration with a capturing wrapper
            var descriptor = new ServiceDescriptor(
                typeof(ILoggerFactory),
                sp =>
                {
                    // Get the original logger factory
                    var loggerFactory = LoggerFactory.Create(loggingBuilder =>
                    {
                        // Copy all the logging configuration from the builder
                        foreach (var service in sp.GetServices<ILoggerProvider>())
                        {
                            // Logger providers are already registered, LoggerFactory will pick them up
                        }
                    });

                    // This is a simpler approach - just capture when resolved
                    loggerFactory.AddSplitLogs();

                    return loggerFactory;
                },
                ServiceLifetime.Singleton);

            // Remove existing ILoggerFactory registration and add ours
            for (int i = builder.Services.Count - 1; i >= 0; i--)
            {
                if (builder.Services[i].ServiceType == typeof(ILoggerFactory))
                {
                    var existing = builder.Services[i];
                    builder.Services[i] = new ServiceDescriptor(
                        typeof(ILoggerFactory),
                        sp =>
                        {
                            ILoggerFactory factory;
                            if (existing.ImplementationFactory != null)
                            {
                                factory = (ILoggerFactory)existing.ImplementationFactory(sp);
                            }
                            else if (existing.ImplementationInstance != null)
                            {
                                factory = (ILoggerFactory)existing.ImplementationInstance;
                            }
                            else
                            {
                                factory = (ILoggerFactory)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType);
                            }

                            // Capture the factory
                            factory.AddSplitLogs();
                            return factory;
                        },
                        existing.Lifetime);
                    break;
                }
            }

            return builder;
        }
    }
}
#endif
