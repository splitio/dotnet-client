using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;

namespace Splitio.Util
{
    public class Helper
    {
        public static List<T> TakeFromList<T>(List<T> items, int size)
        {
            if (items == null) return new List<T>();

            var count = size;

            if (items.Count < size)
            {
                count = items.Count;
            }

            var bulk = items.GetRange(0, count);
            items.RemoveRange(0, count);

            return bulk;
        }

        public static void RecordTelemetrySync(string method, HTTPResult response, ResourceEnum resource, SplitStopwatch clock, ITelemetryRuntimeProducer telemetryRuntimeProducer, ISplitLogger log)
        {
            if (response.IsSuccessStatusCode)
            {
                telemetryRuntimeProducer.RecordSyncLatency(resource, Metrics.Bucket(clock.ElapsedMilliseconds));
                telemetryRuntimeProducer.RecordSuccessfulSync(resource, CurrentTimeHelper.CurrentTimeMillis());
            }
            else
            {
                telemetryRuntimeProducer.RecordSyncError(resource, (int)response.StatusCode);
            }

            log.Debug($"Http status executing {method}: {response.StatusCode}");
        }
    }
}
