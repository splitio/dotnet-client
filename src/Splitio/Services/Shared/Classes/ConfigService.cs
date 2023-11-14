using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Shared.Classes
{
    public class ConfigService : IConfigService
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(ConfigService));
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly IFlagSetsValidator _flagSetsValidator;

        public ConfigService(IWrapperAdapter wrapperAdapter,
            IFlagSetsValidator flagSetsValidator)
        {
            _wrapperAdapter = wrapperAdapter;
            _flagSetsValidator = flagSetsValidator;
        }

        #region Public Methods
        public BaseConfig ReadConfig(ConfigurationOptions config, ConfigTypes configType)
        {
            switch (configType)
            {
                case ConfigTypes.Redis:
                    return ReadRedisConfig(config);
                case ConfigTypes.InMemory:
                default:
                    return ReadInMemoryConfig(config);
            }
        }
        
        public BaseConfig ReadRedisConfig(ConfigurationOptions config)
        {
            var baseConfig = ReadBaseConfig(config, ConfigTypes.Redis);

            baseConfig.ImpressionsMode = config.ImpressionsMode ?? ImpressionsMode.Debug;
            baseConfig.UniqueKeysRefreshRate = 300;
            baseConfig.ImpressionsCounterRefreshRate = 300;
            baseConfig.ImpressionsCountBulkSize = 10000;
            baseConfig.UniqueKeysBulkSize = 10000;

            return baseConfig;
        }

        public SelfRefreshingConfig ReadInMemoryConfig(ConfigurationOptions config)
        {
            var baseConfig = ReadBaseConfig(config, ConfigTypes.InMemory);

            var selfRefreshingConfig = new SelfRefreshingConfig
            {
                Mode = config.Mode,
                SdkVersion = baseConfig.SdkVersion,
                SdkMachineName = baseConfig.SdkMachineName,
                SdkMachineIP = baseConfig.SdkMachineIP,
                LabelsEnabled = baseConfig.LabelsEnabled,
                BfErrorRate = baseConfig.BfErrorRate,
                BfExpectedElements = baseConfig.BfExpectedElements,
                UniqueKeysCacheMaxSize = baseConfig.UniqueKeysCacheMaxSize,
                ImpressionsCounterCacheMaxSize = baseConfig.ImpressionsCounterCacheMaxSize,
                FlagSetsFilter = baseConfig.FlagSetsFilter,
                FlagSetsInvalid = baseConfig.FlagSetsInvalid,
                UniqueKeysBulkSize = 30000,
                ImpressionsCountBulkSize = 30000,
                UniqueKeysRefreshRate = 3600,
                ImpressionsCounterRefreshRate = 1800, // Send bulk impressions count - Refresh rate: 30 min.
                ImpressionsMode = config.ImpressionsMode ?? ImpressionsMode.Optimized,
                SplitsRefreshRate = config.FeaturesRefreshRate ?? 60,
                SegmentRefreshRate = config.SegmentsRefreshRate ?? 60,
                HttpConnectionTimeout = config.ConnectionTimeout ?? 15000,
                HttpReadTimeout = config.ReadTimeout ?? 15000,
                RandomizeRefreshRates = config.RandomizeRefreshRates,ConcurrencyLevel = config.SplitsStorageConcurrencyLevel ?? 4,
                TreatmentLogSize = config.MaxImpressionsLogSize ?? 30000,
                ImpressionsBulkSize = 5000,
                EventLogRefreshRate = config.EventsPushRate ?? 60,
                EventLogSize = config.EventsQueueSize ?? 10000,
                EventsBulkSize = 500,
                EventsFirstPushWindow = config.EventsFirstPushWindow ?? 10,
                NumberOfParalellSegmentTasks = config.NumberOfParalellSegmentTasks ?? 5,
                StreamingEnabled = config.StreamingEnabled ?? true,
                AuthRetryBackoffBase = GetMinimunAllowed(config.AuthRetryBackoffBase ?? 1, 1, "AuthRetryBackoffBase"),
                StreamingReconnectBackoffBase = GetMinimunAllowed(config.StreamingReconnectBackoffBase ?? 1, 1, "StreamingReconnectBackoffBase"),
                TelemetryRefreshRate = GetMinimunAllowed(config.TelemetryRefreshRate ?? 3600, 60, "TelemetryRefreshRate"),
                ImpressionListener = config.ImpressionListener,
                AuthServiceURL = string.IsNullOrEmpty(config.AuthServiceURL) ? Constants.Urls.AuthServiceURL : config.AuthServiceURL,
                BaseUrl = string.IsNullOrEmpty(config.Endpoint) ? Constants.Urls.BaseUrl : config.Endpoint,
                EventsBaseUrl = string.IsNullOrEmpty(config.EventsEndpoint) ? Constants.Urls.EventsBaseUrl : config.EventsEndpoint,
                StreamingServiceURL = string.IsNullOrEmpty(config.StreamingServiceURL) ? Constants.Urls.StreamingServiceURL : config.StreamingServiceURL,
                TelemetryServiceURL = string.IsNullOrEmpty(config.TelemetryServiceURL) ? Constants.Urls.TelemetryServiceURL : config.TelemetryServiceURL,
                SdkStartTime = CurrentTimeHelper.CurrentTimeMillis(),
                OnDemandFetchMaxRetries = 10,
                OnDemandFetchRetryDelayMs = 50,
                ProxyHost = config.ProxyHost,
                ProxyPort = config.ProxyPort
            };

            selfRefreshingConfig.ImpressionsMode = config.ImpressionsMode ?? ImpressionsMode.Optimized;
            selfRefreshingConfig.TreatmentLogRefreshRate = GetImpressionRefreshRate(selfRefreshingConfig.ImpressionsMode, config.ImpressionsRefreshRate);

            return selfRefreshingConfig;
        }
        #endregion

        #region Private Methods
        private BaseConfig ReadBaseConfig(ConfigurationOptions config, ConfigTypes type)
        {
            var data = _wrapperAdapter.ReadConfig(config, _log);
            var flagSetsResult = FlagSetsValidations(config.FlagSetsFilter, type);

            return new BaseConfig
            {
                SdkVersion = data.SdkVersion,
                SdkMachineName = data.SdkMachineName,
                SdkMachineIP = data.SdkMachineIP,
                LabelsEnabled = config.LabelsEnabled ?? true,
                BfExpectedElements = 10000000,
                BfErrorRate = 0.01,
                UniqueKeysCacheMaxSize = 50000,
                ImpressionsCounterCacheMaxSize = 50000,
                FlagSetsFilter = flagSetsResult.FlagSets,
                FlagSetsInvalid = flagSetsResult.Invalid
            };
        }

        private FlagSetsValidationResult FlagSetsValidations(List<string> flagSets, ConfigTypes type)
        {
            if (flagSets == null) return new FlagSetsValidationResult();

            if (type == ConfigTypes.Redis && flagSets.Any())
            {
                _log.Warn("SDK Config: FlagSets filter is not applicable for Consumer modes where the SDK does not keep rollout data in sync. FlagSet filter was discarded.");

                return new FlagSetsValidationResult();
            }

            var cleanup = _flagSetsValidator.Cleanup("SDK Config", flagSets);
            var toReturn = _flagSetsValidator.Items("SDK Config", cleanup);

            return new FlagSetsValidationResult(toReturn, flagSets.Count - toReturn.Count);
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

        private static int GetImpressionRefreshRate(ImpressionsMode impressionsMode, int? impressionsRefreshRate)
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
        #endregion
    }

    public enum ConfigTypes
    {
        InMemory,
        Redis
    }

    public class FlagSetsValidationResult
    {
        public HashSet<string> FlagSets { get; }
        public int Invalid { get; }

        public FlagSetsValidationResult(HashSet<string> flagSets, int invalid)
        {
            FlagSets = flagSets;
            Invalid = invalid;
        }

        public FlagSetsValidationResult()
        {
            FlagSets = new HashSet<string>();
            Invalid = 0;
        }
    }
}
