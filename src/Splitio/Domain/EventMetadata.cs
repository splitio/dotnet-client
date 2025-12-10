using System.Collections.Generic;
using Splitio.Services.InputValidation.Classes;
using System.Linq;

namespace Splitio.Domain
{
    public class EventMetadata
    {
        private readonly Dictionary<string, object> _data;

        public EventMetadata(Dictionary<string, object> data) 
        {
            _data = Santize(data);
        }

        public Dictionary<string, object> GetData() { return _data; }

        public List<string> GetKeys() { return _data.Keys.ToList(); }

        public List<object> GetValues() { return _data.Values.ToList(); }

        public bool ContainKey(string key)
        {
            return _data.ContainsKey(key);
        }

        private Dictionary<string, object> Santize(Dictionary<string, object> data)
        {
            Dictionary<string, object> santizedData = new Dictionary<string, object>();
            foreach (var item in data.Where(x => ValueIsValid(x.Value))) 
            {
                santizedData.Add(item.Key, item.Value);
            }

            return santizedData;
        }

        private static bool ValueIsValid(object value)
        {
            if (!(value is null) && (PropertiesValidator.IsNumeric(value) || (value is bool) || (value is string) || (value is List<string>)))
            {
                return true;
            }

            return false;
        }

    }
}
