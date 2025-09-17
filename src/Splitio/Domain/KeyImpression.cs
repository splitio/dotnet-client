using Newtonsoft.Json;

namespace Splitio.Domain
{
    public class KeyImpression
    {
        public KeyImpression() { }

        public KeyImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey, bool impressionsDisabled, long? previousTime = null, bool optimized = false)
        {
            this.feature = feature;
            keyName = matchingKey;
            this.treatment = treatment;
            this.time = time;
            this.changeNumber = changeNumber;
            this.label = label;
            this.bucketingKey = bucketingKey;
            this.previousTime = previousTime;
            ImpressionsDisabled = impressionsDisabled;
            this.optimized = optimized;
        }

        [JsonIgnore]
        public string feature { get; set; }
        public string keyName { get; set; }
        public string treatment { get; set; }
        public long time { get; set; }
        public long? changeNumber { get; set; }
        public string label { get; set; }
        public string bucketingKey { get; set; }
        public long? previousTime { get; set; }
        [JsonIgnore]
        public bool optimized { get; set; }
        [JsonIgnore]
        public bool ImpressionsDisabled { get; set; }
        public string properties { get; set; }
    }
}
