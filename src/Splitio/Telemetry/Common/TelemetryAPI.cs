using Newtonsoft.Json;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio.Telemetry.Common
{
    public class TelemetryAPI : ITelemetryAPI
    {
        private const string ConfigURL = "/metrics/config";
        private const string UsageURL = "/metrics/usage";
        private const string UniqueKeysURL = "/keys/ss";

        private readonly ISplitioHttpClient _splitioHttpClient;
        private readonly ISplitLogger _log;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _telemetryURL;

        public TelemetryAPI(ISplitioHttpClient splitioHttpClient,
            string telemetryURL,
            ITelemetryRuntimeProducer telemetryRuntimeProducer)
        {
            _splitioHttpClient = splitioHttpClient;
            _telemetryURL = telemetryURL;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _log = WrapperAdapter.Instance().GetLogger(typeof(TelemetryAPI));
        }

        #region Public Methods
        public async Task RecordConfigInitAsync(Config init)
        {
            await ExecutePost(ConfigURL, init, nameof(RecordConfigInitAsync));
        }

        public async Task RecordStatsAsync(Stats stats)
        {
            await ExecutePost(UsageURL, stats, nameof(RecordStatsAsync));
        }

        public async Task RecordUniqueKeysAsync(UniqueKeys uniqueKeys)
        {
            await ExecutePost(UniqueKeysURL, uniqueKeys, nameof(RecordUniqueKeysAsync));
        }
        #endregion

        #region Private Methods
        private async Task ExecutePost(string url, object data, string method)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var jsonData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                var response = await _splitioHttpClient.PostAsync($"{_telemetryURL}{url}", jsonData);

                Util.Helper.RecordTelemetrySync(method, response, ResourceEnum.TelemetrySync, clock, _telemetryRuntimeProducer, _log);
            }
        }
        #endregion
    }
}
