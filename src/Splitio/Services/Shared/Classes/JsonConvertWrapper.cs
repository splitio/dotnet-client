using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Splitio.Services.Shared.Classes
{
    public static class JsonConvertWrapper
    {
        private static readonly JsonSerializerSettings _defaultSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            }
        };

        private static readonly JsonSerializerSettings _nullValueHandlingSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            }
        };

        public static string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value, _defaultSerializerSettings);
        }

        public static string SerializeObjectIgnoreNullValue(object value)
        {
            return JsonConvert.SerializeObject(value, _nullValueHandlingSettings);
        }

        public static T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, _defaultSerializerSettings);
        }
    }
}
