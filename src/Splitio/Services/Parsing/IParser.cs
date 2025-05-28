using Splitio.Services.Cache.Interfaces;

namespace Splitio.Services.Parsing.Interfaces
{
    public interface IParser<T,P> where T : class where P : class
    {
        P Parse(T entity, IRuleBasedSegmentCacheConsumer ruleBasedSegmentCache);
    }
}
