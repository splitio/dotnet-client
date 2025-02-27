namespace Splitio.Domain
{
    public class FetchOptions
    {
        public long FeatureFlagsSince { get; set; }
        public long RuleBasedSegmentsSince { get; set; }
        public long? Till { get; set; }
        public bool CacheControlHeaders { get; set; }
    }
}
