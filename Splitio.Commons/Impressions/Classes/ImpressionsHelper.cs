using System;

namespace Splitio.Commons.Impressions.Classes
{
    public class ImpressionsHelper
    {
        public const long TimeIintervalMs = 3600L * 1000L;

        public static long TruncateTimeFrame(long timestampInMs)
        {
            return timestampInMs - (timestampInMs % TimeIintervalMs);
        }
    }
}
