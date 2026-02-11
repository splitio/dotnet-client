using Newtonsoft.Json;

namespace Splitio.Domain
{
    public class KeyImpressionMetadataDto
    {
        [JsonProperty("s")]
        private readonly string _sdkVersion;
        [JsonProperty("i")]
        private readonly string _machineIp;
        [JsonProperty("n")]
        private readonly string _machineName;

        public KeyImpressionMetadataDto(string sdkVersion, string machineIp, string machineName)
        {
            _sdkVersion = sdkVersion;
            _machineIp = machineIp;
            _machineName = machineName;
        }
    }

    public class KeyImpressionItemDto
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

        public KeyImpressionItemDto(KeyImpression impression)
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

    public class KeyImpressionDto
    {
        [JsonProperty("m")]
        private readonly KeyImpressionMetadataDto _metadata;
        [JsonProperty("i")]
        private readonly KeyImpressionItemDto _item;

        public KeyImpressionDto(KeyImpression impression, string sdkVersion, string machineIp, string machineName)
        {
            _metadata = new KeyImpressionMetadataDto(sdkVersion, machineIp, machineName);
            _item = new KeyImpressionItemDto(impression);
        }
    }
}