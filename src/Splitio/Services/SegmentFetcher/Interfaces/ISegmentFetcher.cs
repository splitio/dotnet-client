using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Interfaces
{
    public interface ISegmentFetcher
    {
        Task InitializeSegmentAsync(string name);
    }
}
