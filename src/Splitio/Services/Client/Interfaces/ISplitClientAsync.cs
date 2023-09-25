using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Interfaces
{
    public interface ISplitClientAsync
    {
        Task<string> GetTreatmentAsync(string key, string feature, Dictionary<string, object> attributes = null);
        Task<string> GetTreatmentAsync(Key key, string feature, Dictionary<string, object> attributes = null);
        Task<Dictionary<string, string>> GetTreatmentsAsync(string key, List<string> features, Dictionary<string, object> attributes = null);
        Task<Dictionary<string, string>> GetTreatmentsAsync(Key key, List<string> features, Dictionary<string, object> attributes = null);
        Task<SplitResult> GetTreatmentWithConfigAsync(string key, string feature, Dictionary<string, object> attributes = null);
        Task<SplitResult> GetTreatmentWithConfigAsync(Key key, string feature, Dictionary<string, object> attributes = null);
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(string key, List<string> features, Dictionary<string, object> attributes = null);
        Task<Dictionary<string, SplitResult>> GetTreatmentsWithConfigAsync(Key key, List<string> features, Dictionary<string, object> attributes = null);
        Task<bool> TrackAsync(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null);
        Task DestroyAsync();
    }
}
