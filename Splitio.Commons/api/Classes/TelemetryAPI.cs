using Newtonsoft.Json;
using Splitio.Commons.api.Interfaces;
using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Telemetry.Domain;
using Splitio.Commons.Telemetry.Domain.Enums;
using Splitio.Commons.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Classes
{
    public class TelemetryAPI : ITelemetryAPI
    {
        private const string ConfigURL = "/metrics/config";
        private const string UsageURL = "/metrics/usage";
        private const string UniqueKeysURL = "/keys/ss";

        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(TelemetryAPI));

        private readonly ISplitioHttpClient _splitioHttpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _telemetryURL;

        public TelemetryAPI(ISplitioHttpClient splitioHttpClient,
            string telemetryURL,
            ITelemetryRuntimeProducer telemetryRuntimeProducer)
        {
            _splitioHttpClient = splitioHttpClient;
            _telemetryURL = telemetryURL;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
        }

        #region Public Methods
        public async Task RecordConfigInitAsync(Config init)
        {
            await ExecutePostAsync(ConfigURL, init, nameof(RecordConfigInitAsync));
        }

        public async Task RecordStatsAsync(Stats stats)
        {
            await ExecutePostAsync(UsageURL, stats, nameof(RecordStatsAsync));
        }

        public async Task RecordUniqueKeysAsync(UniqueKeys uniqueKeys)
        {
            await ExecutePostAsync(UniqueKeysURL, uniqueKeys, nameof(RecordUniqueKeysAsync));
        }
        #endregion

        #region Private Methods
        private async Task ExecutePostAsync(string url, object data, string method)
        {
            using (var clock = new SplitStopwatch())
            {
                clock.Start();

                var jsonData = JsonConvertWrapper.SerializeObjectIgnoreNullValue(data);

                var response = await _splitioHttpClient.PostAsync($"{_telemetryURL}{url}", jsonData);

                Helper.RecordTelemetrySync(method, response, ResourceEnum.TelemetrySync, clock, _telemetryRuntimeProducer, _log);
            }
        }
        #endregion
    }
}
