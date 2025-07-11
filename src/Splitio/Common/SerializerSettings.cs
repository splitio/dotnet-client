using Newtonsoft.Json;

namespace Splitio.Common
{
    static class SerializerSettings
    {
        public static JsonSerializerSettings DefaultSerializerSettings { get; } = new JsonSerializerSettings();
    }
}