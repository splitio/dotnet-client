using System.Collections.Generic;

namespace Splitio.Commons.Domain
{
    public class EvaluationOptions
    {
        public EvaluationOptions()
        {
            Properties = null;
        }
        public EvaluationOptions(Dictionary<string, object> Properties)
        {
            this.Properties = Properties;
        }
        public Dictionary<string, object> Properties { get; set; }
    }
}