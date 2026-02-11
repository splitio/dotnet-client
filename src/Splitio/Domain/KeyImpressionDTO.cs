using Newtonsoft.Json;

namespace Splitio.Domain
{
    public class KeyImpressionMetadataDTO
    {
        [JsonProperty("s")]
        private readonly string _sdkVersion;
        [JsonProperty("i")]
        private readonly string _machineIp;
        [JsonProperty("n")]
        private readonly string _machineName;

        public KeyImpressionMetadataDTO(string sdkVersion, string machineIp, string machineName)
        {
            _sdkVersion = sdkVersion;
            _machineIp = machineIp;
            _machineName = machineName;
        }
    }

    public class KeyImpressionItemDTO
    {
        [JsonProperty("f")]
        private readonly string _feature;
        [JsonProperty("k")]
        private readonly string _keyName;
        [JsonProperty("t")]
        private readonly string _treatment;
        [JsonProperty("m")]
        private readonly long _time;
        [JsonProperty("c")]
        private readonly long? _changeNumber;
        [JsonProperty("r")]
        private readonly string _label;
        [JsonProperty("b")]
        private readonly string _bucketingKey;
        [JsonProperty("pt")]
        private readonly long? _previousTime;
        
        [JsonProperty(PropertyName = "properties", NullValueHandling = NullValueHandling.Ignore)]
        private readonly string _properties;

        public KeyImpressionItemDTO(KeyImpression impression)
        {
            _feature = impression.feature;
            _keyName = impression.keyName;
            _treatment = impression.treatment;
            _time = impression.time;
            _changeNumber = impression.changeNumber;
            _label = impression.label;
            _bucketingKey = impression.bucketingKey;
            _previousTime = impression.previousTime;
            _properties = impression.properties;
        }
    }

    public class KeyImpressionDTO
    {
        [JsonProperty("m")]
        private readonly KeyImpressionMetadataDTO _metadata;
        [JsonProperty("i")]
        private readonly KeyImpressionItemDTO _item;

        public KeyImpressionDTO(KeyImpression impression, string sdkVersion, string machineIp, string machineName)
        {
            _metadata = new KeyImpressionMetadataDTO(sdkVersion, machineIp, machineName);
            _item = new KeyImpressionItemDTO(impression);
        }
    }
}