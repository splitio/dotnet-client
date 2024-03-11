
namespace Splitio.Domain
{
    public class Matcher
    {
        public KeySelector KeySelector { get; set; }
        public string MatcherType { get; set; }
        public bool Negate { get; set; }
        public UserDefinedSegmentData UserDefinedSegmentMatcherData { get; set; }
        public WhitelistData WhitelistMatcherData { get; set; }
        public UnaryNumericData UnaryNumericMatcherData { get; set; }
        public BetweenData BetweenMatcherData { get; set; }
        public DependencyData DependencyMatcherData { get; set; }
        public bool? BooleanMatcherData { get; set; }
        public string StringMatcherData { get; set; }
    }
}
