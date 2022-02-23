using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Telemetry.Domain
{
    public class UniqueKeys
    {
        [JsonProperty("mtks")]
        public List<Mtks> Mtks { get; }

        public UniqueKeys(ConcurrentDictionary<string, HashSet<string>> values)
        {
            Mtks = values
                .Select(v => new Mtks(v.Key, v.Value))
                .ToList();
        }
    }

    public class Mtks
    {
        [JsonProperty("feature")]
        public string Feature { get; }
        [JsonProperty("keys")]
        public HashSet<string> Keys { get; }

        public Mtks(string feature, HashSet<string> keys)
        {
            Feature = feature;
            Keys = keys;
        }
    }
}
