﻿using Splitio.Services.Cache.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISynchronizer
    {
        Task<bool> SyncAllAsync();
        Task SynchronizeSplitsAsync(long targetChangeNumber, ICacheConsumer cacheConsumer);
        Task SynchronizeSegmentAsync(string segmentName, long targetChangeNumber);
        void StartPeriodicFetching();
        Task StopPeriodicFetchingAsync();
        void StartPeriodicDataRecording();
        Task StopPeriodicDataRecordingAsync();
        void ClearFetchersCache();
    }
}
