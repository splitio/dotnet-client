using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;

namespace Splitio.Services.Shared.Classes
{
    public class ConfigService : IConfigService
    {
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISplitLogger _log;

        public ConfigService(IWrapperAdapter wrapperAdapter,
            ISplitLogger log)
        {
            _wrapperAdapter = wrapperAdapter;
            _log = log;
        }

        public BaseConfig ReadConfig(ConfigurationOptions config, ConfingTypes configType)
        {
            switch (configType)
            {
                case ConfingTypes.Redis:
                    return ReadBaseConfig(config);
                case ConfingTypes.InMemory:
                default:
                    return ReadInMemoryConfig(config);
            }
        }

        public BaseConfig ReadBaseConfig(ConfigurationOptions config)
        {
            var data = _wrapperAdapter.ReadConfig(config, _log);

            return new BaseConfig
            {
                SdkVersion = data.SdkVersion,
                SdkMachineName = data.SdkMachineName,
                SdkMachineIP = data.SdkMachineIP,
                LabelsEnabled = config.LabelsEnabled ?? true
            };
        }

        public SelfRefreshingConfig ReadInMemoryConfig(ConfigurationOptions config)
        {
            var baseConfig = ReadBaseConfig(config);

            var selfRefreshingConfig = new SelfRefreshingConfig
            {
                Mode = config.Mode,
                SdkVersion = baseConfig.SdkVersion,
                SdkMachineName = baseConfig.SdkMachineName,
                SdkMachineIP = baseConfig.SdkMachineIP,
                LabelsEnabled = baseConfig.LabelsEnabled,                
                SplitsRefreshRate = config.FeaturesRefreshRate ?? 5,
                SegmentRefreshRate = config.SegmentsRefreshRate ?? 60,
                HttpConnectionTimeout = config.ConnectionTimeout ?? 15000,
                HttpReadTimeout = config.ReadTimeout ?? 15000,
                RandomizeRefreshRates = config.RandomizeRefreshRates,ConcurrencyLevel = config.SplitsStorageConcurrencyLevel ?? 4,
                TreatmentLogSize = config.MaxImpressionsLogSize ?? 30000,
                EventLogRefreshRate = config.EventsPushRate ?? 60,
                EventLogSize = config.EventsQueueSize ?? 5000,
                EventsFirstPushWindow = config.EventsFirstPushWindow ?? 10,
                NumberOfParalellSegmentTasks = config.NumberOfParalellSegmentTasks ?? 5,
                StreamingEnabled = config.StreamingEnabled ?? true,
                AuthRetryBackoffBase = GetMinimunAllowed(config.AuthRetryBackoffBase ?? 1, 1, "AuthRetryBackoffBase"),
                StreamingReconnectBackoffBase = GetMinimunAllowed(config.StreamingReconnectBackoffBase ?? 1, 1, "StreamingReconnectBackoffBase"),
                ImpressionsMode = config.ImpressionsMode ?? ImpressionsMode.Optimized,
                TelemetryRefreshRate = GetMinimunAllowed(config.TelemetryRefreshRate ?? 3600, 60, "TelemetryRefreshRate"),
                ImpressionListener = config.ImpressionListener,
                AuthServiceURL = string.IsNullOrEmpty(config.AuthServiceURL) ? Constants.Urls.AuthServiceURL : config.AuthServiceURL,
                BaseUrl = string.IsNullOrEmpty(config.Endpoint) ? Constants.Urls.BaseUrl : config.Endpoint,
                EventsBaseUrl = string.IsNullOrEmpty(config.EventsEndpoint) ? Constants.Urls.EventsBaseUrl : config.EventsEndpoint,
                StreamingServiceURL = string.IsNullOrEmpty(config.StreamingServiceURL) ? Constants.Urls.StreamingServiceURL : config.StreamingServiceURL,
                TelemetryServiceURL = string.IsNullOrEmpty(config.TelemetryServiceURL) ? Constants.Urls.TelemetryServiceURL : config.TelemetryServiceURL,
                SdkStartTime = CurrentTimeHelper.CurrentTimeMillis(),
                OnDemandFetchMaxRetries = 10,
                OnDemandFetchRetryDelayMs = 50
            };

            selfRefreshingConfig.TreatmentLogRefreshRate = GetImpressionRefreshRate(selfRefreshingConfig.ImpressionsMode, config.ImpressionsRefreshRate);

            return selfRefreshingConfig;
        }

        private int GetMinimunAllowed(int value, int minAllowed, string configName)
        {
            if (value < minAllowed)
            {
                _log.Warn($"{configName} minimum allowed value: {minAllowed}");

                return minAllowed;
            }

            return value;
        }

        private int GetImpressionRefreshRate(ImpressionsMode impressionsMode, int? impressionsRefreshRate)
        {
            switch (impressionsMode)
            {
                case ImpressionsMode.Debug:
                    return impressionsRefreshRate == null || impressionsRefreshRate <= 0 ? 60 : impressionsRefreshRate.Value;
                case ImpressionsMode.Optimized:
                default:
                    return impressionsRefreshRate == null || impressionsRefreshRate <= 0 ? 300 : Math.Max(60, impressionsRefreshRate.Value);
            }
        }
    }

    public enum ConfingTypes
    {
        InMemory,
        Redis
    }
}
