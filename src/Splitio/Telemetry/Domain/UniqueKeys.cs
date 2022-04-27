using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class UniqueKeys
    {
        [JsonProperty("keys")]
        public List<Mtks> Keys { get; }

        public UniqueKeys(List<Mtks> values)
        {
            Keys = values;
        }
    }

    public class Mtks
    {
        [JsonProperty("f")]
        public string Feature { get; }
        [JsonProperty("ks")]
        public HashSet<string> Keys { get; }

        public Mtks(string feature, HashSet<string> keys)
        {
            Feature = feature;
            Keys = keys;
        }
    }
}
