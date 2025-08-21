using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EvaluationOptions
    {
        public EvaluationOptions()
        {
            this.Properties = null;
        }
        public EvaluationOptions(Dictionary<string, object> Properties)
        {
            this.Properties = Properties;
        }
        public Dictionary<string, object> Properties { get; set; }
    }
}