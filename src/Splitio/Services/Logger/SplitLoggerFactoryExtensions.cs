#if NETSTANDARD2_0 || NET6_0 || NET5_0
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
    }
}
#endif