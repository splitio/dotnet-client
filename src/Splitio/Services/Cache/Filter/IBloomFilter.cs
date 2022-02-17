namespace Splitio.Services.Cache.Filter
{
    public interface IBloomFilter
    {
        bool Add(string data);
        bool Contains(string data);
        void Clear();
    }
}
