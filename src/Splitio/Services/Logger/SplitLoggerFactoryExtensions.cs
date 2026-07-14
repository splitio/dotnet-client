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
        /// The logger factory will be captured when it is first resolved from DI.
        /// Only wraps the default ASP.NET Core factory registration.
        /// </summary>
        public static ILoggingBuilder AddSplitLogs(this ILoggingBuilder builder)
        {
            // Find and wrap only the default ILoggerFactory registration
            ServiceDescriptor loggerFactoryDescriptor = null;
            for (int i = 0; i < builder.Services.Count; i++)
            {
                if (builder.Services[i].ServiceType == typeof(ILoggerFactory))
                {
                    loggerFactoryDescriptor = builder.Services[i];

                    // Wrap this specific registration to capture the factory when resolved
                    var wrappedDescriptor = ServiceDescriptor.Describe(
                        typeof(ILoggerFactory),
                        sp =>
                        {
                            ILoggerFactory factory;

                            // Resolve the original factory based on how it was registered
                            if (loggerFactoryDescriptor.ImplementationFactory != null)
                            {
                                factory = (ILoggerFactory)loggerFactoryDescriptor.ImplementationFactory(sp);
                            }
                            else if (loggerFactoryDescriptor.ImplementationInstance != null)
                            {
                                factory = (ILoggerFactory)loggerFactoryDescriptor.ImplementationInstance;
                            }
                            else
                            {
                                factory = (ILoggerFactory)ActivatorUtilities.CreateInstance(sp, loggerFactoryDescriptor.ImplementationType);
                            }

                            // Capture the factory for Split logging
                            factory.AddSplitLogs();

                            return factory;
                        },
                        loggerFactoryDescriptor.Lifetime);

                    // Replace only this registration
                    builder.Services[i] = wrappedDescriptor;
                    break;
                }
            }

            return builder;
        }
    }
}
#endif
