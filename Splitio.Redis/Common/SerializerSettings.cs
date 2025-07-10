using Newtonsoft.Json;

namespace Splitio.Redis.Common
{
    static class SerializerSettings
    {
        public static JsonSerializerSettings DefaultSerializerSettings { get; } = new JsonSerializerSettings();
    }
}