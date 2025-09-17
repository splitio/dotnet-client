using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;

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

        public static bool HasNonASCIICharacters(string input)
        {
            foreach (var c in input)
            {
                if (c > 127) return true;
            }

            return false;
        }

        public static List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
