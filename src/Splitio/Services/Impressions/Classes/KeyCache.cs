using System;

namespace Splitio.Services.Impressions.Classes
{
    public class KeyCache
    {
        public string SplitName { get; set; }
        public long TimeFrame { get; set; }

        public KeyCache(string splitName, long timeFrame)
        {
            SplitName = splitName;
            TimeFrame = ImpressionsHelper.TruncateTimeFrame(timeFrame);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(SplitName, TimeFrame).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || obj.GetType() != GetType()) return false;

            var key = (KeyCache)obj;
            return key.SplitName.Equals(SplitName) && key.TimeFrame.Equals(TimeFrame);
        }
    }
}
