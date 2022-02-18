namespace Splitio.Services.Impressions.Interfaces
{
    public interface IUniqueKeysTracker
    {
        void Start();
        void Stop();
        void Track(string key, string featureName);
    }
}
