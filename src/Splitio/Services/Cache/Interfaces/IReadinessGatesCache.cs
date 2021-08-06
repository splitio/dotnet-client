namespace Splitio.Services.Cache.Interfaces
{
    public interface IReadinessGatesCache
    {
        bool AreSplitsReady(int milliseconds);
        bool IsSDKReady(int milliseconds);
        bool RegisterSegment(string segmentName);
        void SegmentIsReady(string segmentName);
        void SplitsAreReady();
        void SdkInternalReady();
        void WaitUntilSdkInternalReady();
    }
}
