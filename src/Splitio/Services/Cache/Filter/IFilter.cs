namespace Splitio.Services.Cache.Filter
{
    public interface IFilter
    {
        bool Add(string data);
        bool Contains(string data);
        void Clear();
    }
}
