using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splitio.Domain
{
    public class ClusterNodes
    {
        public string KeyHashTag {  get; set; }
        public List<string> EndPoints { get; set; }

        public ClusterNodes(List<string> endPoints, string keyHashTag) 
        {
            EndPoints = endPoints;
            KeyHashTag = keyHashTag;
        }

    }
}
