#if NET_LATEST
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
    }
}
#endif