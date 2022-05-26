using Newtonsoft.Json;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsCountModel
    {
        [JsonProperty("f")]
        public string SplitName { get; set; }
        [JsonProperty("m")]
        public long TimeFrame { get; set; }
        [JsonProperty("rc")]
        public int Count { get; set; }

        public ImpressionsCountModel(KeyCache keyCache, int count)
        {
            SplitName = keyCache.SplitName;
            TimeFrame = keyCache.TimeFrame;
            Count = count;
        }
    }
}
