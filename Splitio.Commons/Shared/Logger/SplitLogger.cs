#if NET_LATEST
using Microsoft.Extensions.Logging;
using Splitio;
using Splitio.Commons.Shared.Logger;
#endif

namespace Splitio.Commons.Shared.Logger
{
    public static class SplitLogger
    {
        public static ISplitLogger Console() => new SplitLogging(Level.Info, System.Console.Out);
        public static ISplitLogger Console(Level level) => new SplitLogging(level, System.Console.Out);
        public static ISplitLogger TextWriter(Level level, System.IO.TextWriter textWriter) => new SplitLogging(level, textWriter);
#if NET_LATEST
        private const string DefaultType = "SplitClient";
        public static ISplitLogger MicrosoftExtensionsLogging(ILoggerFactory loggerFactory) => new MicrosoftExtensionsLogging(loggerFactory, DefaultType);
#endif
    }
}
