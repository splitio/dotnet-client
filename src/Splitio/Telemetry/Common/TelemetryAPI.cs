using Newtonsoft.Json;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Net;
using System.Threading.Tasks;

namespace Splitio.Telemetry.Common
{
    public class TelemetryAPI : ITelemetryAPI
    {
        private const string ConfigURL = "/metrics/config";
        private const string UsageURL = "/metrics/usage";

        private readonly ISplitioHttpClient _splitioHttpClient;
        private readonly ISplitLogger _log;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _telemetryURL;

        public TelemetryAPI(ISplitioHttpClient splitioHttpClient,
            string telemetryURL,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISplitLogger log = null)
        {
            _splitioHttpClient = splitioHttpClient;
            _telemetryURL = telemetryURL;
            _log = log ?? WrapperAdapter.GetLogger(typeof(TelemetryAPI));
        }

        #region Public Methods
        public async void RecordConfigInit(Config init)
        {
            await ExecutePost(ConfigURL, init, nameof(RecordConfigInit));
        }

        public async void RecordStats(Stats stats)
        {
            await ExecutePost(UsageURL, stats, nameof(RecordStats));
        }
        #endregion        

        #region Private Methods
        private async Task ExecutePost(string url, object data, string method)
        {
            var jsonData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var response = await _splitioHttpClient.PostAsync($"{_telemetryURL}{url}", jsonData);

            if ((int)response.statusCode < (int)HttpStatusCode.OK || (int)response.statusCode >= (int)HttpStatusCode.Ambiguous)
            {
                _log.Error($"Http status executing {method}: {response.statusCode} - {response.content}");
                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.TelemetrySync, (int)response.statusCode);
            }
        }
        #endregion
    }
}
