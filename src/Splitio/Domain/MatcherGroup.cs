using System.Collections.Generic;

namespace Splitio.Domain
{
    public class MatcherGroup
    {
        public string Combiner { get; set; }
        public List<Matcher> Matchers {get; set;}
    }
}
