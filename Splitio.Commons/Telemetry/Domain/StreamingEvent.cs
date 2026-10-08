using Newtonsoft.Json;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Telemetry.Domain.Enums;

namespace Splitio.Commons.Telemetry.Domain
{
    public class StreamingEvent
    {
        [JsonProperty("e")]
        public int Type { get; set; }
        [JsonProperty("d")]
        public long Data { get; set; }
        [JsonProperty("t")]
        public long Timestamp { get; set; }

        public StreamingEvent(EventTypeEnum type, long data)
        {
            Timestamp = CurrentTimeHelper.CurrentTimeMillis();
            Type = (int)type;
            Data = data;
        }

        public StreamingEvent(EventTypeEnum type)
        {
            Timestamp = CurrentTimeHelper.CurrentTimeMillis();
            Type = (int)type;
        }

        public override string ToString()
        {
            return $"{Type}::{Data}::{Timestamp}::{GetHashCode()}";
        }
    }
}
