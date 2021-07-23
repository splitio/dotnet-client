using System.Collections.Generic;

namespace Splitio.Domain
{
    public class SyncResult
    {
        public bool Success { get; private set; }
        public int RemainingAttempts { get; private set; }
        public IList<string> SegmentNames { get; private set; }

        public SyncResult(bool success, int remainingAttempts)
        {
            Success = success;
            RemainingAttempts = remainingAttempts;
            SegmentNames = null;
        }

        public SyncResult(bool success, int remainingAttempts, IList<string> segmentNames)
        {
            Success = success;
            RemainingAttempts = remainingAttempts;
            SegmentNames = segmentNames;
        }        
    }
}
