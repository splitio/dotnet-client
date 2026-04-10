#if NET_LATEST
using Microsoft.Extensions.Logging;
#endif
using System;

namespace Splitio.Services.Logger
{
    /// <summary>
    /// Delegate for creating custom logger instances
    /// </summary>
    /// <returns>An instance of ISplitLogger</returns>
    public delegate ISplitLogger SplitLoggerBuilder();

    public static class SplitLogger
    {
        public static ISplitLogger Console() => new SplitLogging(Level.Info, System.Console.Out);
        public static ISplitLogger Console(Level level) => new SplitLogging(level, System.Console.Out);
        public static ISplitLogger TextWriter(Level level, System.IO.TextWriter textWriter) => new SplitLogging(level, textWriter);
#if NET_LATEST
        private const string DefaultType = "SplitClient";
        public static ISplitLogger MicrosoftExtensionsLogging(ILoggerFactory loggerFactory) => new MicrosoftExtensionsLogging(loggerFactory, DefaultType);
#endif

        /// <summary>
        /// Create a logger using a custom builder function
        /// </summary>
        /// <param name="builder">Function that returns an ISplitLogger instance</param>
        /// <returns>The logger instance created by the builder</returns>
        public static ISplitLogger Custom(SplitLoggerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder();
        }
    }
}
