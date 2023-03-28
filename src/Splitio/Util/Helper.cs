using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Net;

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

        public static void RecordTelemetrySync(string method, HttpStatusCode statusCode, string content, ResourceEnum resource, SplitStopwatch clock, ITelemetryRuntimeProducer telemetryRuntimeProducer, ISplitLogger log)
        {
            if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.Ambiguous)
            {
                telemetryRuntimeProducer.RecordSyncLatency(resource, Metrics.Bucket(clock.ElapsedMilliseconds));
                telemetryRuntimeProducer.RecordSuccessfulSync(resource, CurrentTimeHelper.CurrentTimeMillis());
            }
            else
            {
                log.Error($"Http status executing {method}: {statusCode} - {content}");
                telemetryRuntimeProducer.RecordSyncError(resource, (int)statusCode);
            }
        }
    }
}
