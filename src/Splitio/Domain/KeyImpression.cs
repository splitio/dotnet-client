using Newtonsoft.Json;

namespace Splitio.Domain
{
    public class KeyImpression
    {
        public KeyImpression() { }

        public KeyImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey, bool impressionsDisabled, long? previousTime = null, bool optimized = false)
        {
            Feature = feature;
            KeyName = matchingKey;
            Treatment = treatment;
            Time = time;
            ChangeNumber = changeNumber;
            Label = label;
            BucketingKey = bucketingKey;
            PreviousTime = previousTime;
            ImpressionsDisabled = impressionsDisabled;
            Optimized = optimized;
        }

        [JsonIgnore]
        public string Feature { get; set; }
        public string KeyName { get; set; }
        public string Treatment { get; set; }
        public long Time { get; set; }
        public long? ChangeNumber { get; set; }
        public string Label { get; set; }
        public string BucketingKey { get; set; }
        public long? PreviousTime { get; set; }
        [JsonIgnore]
        public bool Optimized { get; set; }
        [JsonIgnore]
        public bool ImpressionsDisabled { get; set; }
    }
}
