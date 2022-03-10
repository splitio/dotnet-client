using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class UniqueKeys
    {
        [JsonProperty("mtks")]
        public List<Mtks> Mtks { get; }

        public UniqueKeys(List<Mtks> values)
        {
            Mtks = values;
        }
    }

    public class Mtks
    {
        [JsonProperty("f")]
        public string Feature { get; }
        [JsonProperty("k")]
        public HashSet<string> Keys { get; }

        public Mtks(string feature, HashSet<string> keys)
        {
            Feature = feature;
            Keys = keys;
        }
    }
}
