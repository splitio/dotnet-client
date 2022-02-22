namespace Splitio.Services.Cache.Filter
{
    public interface IFilterAdapter
    {
        bool Add(string featureName, string key);
        bool Contains(string featureName, string key);
        void Clear();
    }
}
