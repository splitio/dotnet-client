using System.Collections.Generic;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : ITelemetryStorage
    {
        public void RecordException(MethodEnum method)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSyncLatency(HttpLatenciesEnum path, int bucket)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        #region Not Implemented Methods
        public void AddTag(string tag)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetBURTimeouts()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetEventsStats(EventsDataRecordsEnum data)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetImpressionsStats(ImpressionsDataRecordsEnum data)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public LastSynchronization GetLastSynchronizations()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetNonReadyUsages()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetSessionLength()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long PopAuthRejections()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public MethodExceptions PopExceptions()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public HTTPErrors PopHttpErrors()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public HTTPLatencies PopHttpLatencies()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public MethodLatencies PopLatencies()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public IList<string> PopTags()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long PopTokenRefreshes()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordAuthRejections()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordBURTimeout()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordEventsStats(EventsDataRecordsEnum data, long count)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordImpressionsStats(ImpressionsDataRecordsEnum data, long count)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordNonReadyUsages()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSessionLength(long session)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSuccessfulSync(LastSynchronizationRecordsEnum method, long timestamp)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSyncError(ResourceEnum resuource, int status)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordTokenRefreshes()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }
        #endregion
    }
}
