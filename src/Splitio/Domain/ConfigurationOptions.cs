using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using System;
using System.Collections.Generic;
using Splitio.Services.Localhost;

namespace Splitio.Services.Client.Classes
{
    public class ConfigurationOptions
    {
        public Mode Mode { get; set; }

        /// <summary>
        /// Maximum size for impressions stored in memory.
        /// default to 30000 if not set.
        /// </summary>
        public int? MaxImpressionsLogSize { get; set; }

        public int? EventsFirstPushWindow { get; set; }

        /// <summary>
        /// Maximum size for track events stored in memory.
        /// default to 10000 if not set.
        /// </summary>
        public int? EventsQueueSize { get; set; }

        /// <summary>
        /// Set Http Connection Timeout in milliseconds.
        /// default to 15000 milliseconds if not set.
        /// </summary>
        public long? ConnectionTimeout { get; set; }

        /// <summary>
        /// Set Http Read Timeout in milliseconds.
        /// default to 15000 milliseconds if not set.
        /// </summary>
        public long? ReadTimeout { get; set; }

        [Obsolete]
        public int? MaxMetricsCountCallsBeforeFlush { get; set; }

        public int? SplitsStorageConcurrencyLevel { get; set; }

        public string SdkMachineName { get; set; }

        public string SdkMachineIP { get; set; }

        /// <summary>
        /// Number of Tasks to use for downloading Segments concurrently.
        /// Defaults to 5 if not set.
        /// </summary>
        public int? NumberOfParalellSegmentTasks { get; set; }

        /// <summary>
        /// Enable/disable labels from being sent to the Harness servers. Labels may contain sensitive information.
        /// Defaults to true if not set.
        /// </summary>
        public bool? LabelsEnabled { get; set; }

        /// <summary>
        /// Set Custom impression listener class implementing the IImpressionListener interface
        /// </summary>
        public IImpressionListener ImpressionListener { get; set; }

        /// <summary>
        /// Set a cache adapter other than InMemory, use to initiate the SDK with support for Redis Cluster
        /// </summary>
        public CacheAdapterConfigurationOptions CacheAdapterConfig { get; set; }

        /// <summary>
        /// Disable machine IP and Hostname from being sent to Harness servers. IP and Hostname may contain sensitive information.
        /// Defaults to true if not set.
        /// </summary>
        public bool? IPAddressesEnabled { get; set; }

        /// <summary>
        /// Boolean flag to enable the streaming service as default synchronization mechanism.
        /// Defaults to true if not set.
        /// </summary>
        public bool? StreamingEnabled { get; set; }

        public int? AuthRetryBackoffBase { get; set; }

        public int? StreamingReconnectBackoffBase { get; set; }

        /// <summary>
        /// Defines how impressions are queued on the SDK. Supported modes are OPTIMIZED, NONE, and DEBUG.
        /// Defaults to Optimized if not set.
        /// </summary>
        public ImpressionsMode? ImpressionsMode { get; set; }

        public bool RandomizeRefreshRates { get; set; }

        /// <summary>
        /// Set a custom logger class implementing ISplitLogger 
        /// </summary>
        public ISplitLogger Logger { get; set; }

        /// <summary>
        /// This setting allows the SDK to only synchronize the feature flags in the specified flag sets.
        /// </summary>
        public List<string> FlagSetsFilter { get; set; }

        /// <summary>
        /// Set Fallback treatments configuration. Fallback treatments are returned by an FME SDK when your application requests a treatment for a feature flag but the SDK cannot determine the value through normal evaluation.
        /// </summary>
        public FallbackTreatmentsConfiguration FallbackTreatments { get; set; }

        // Urls.
        public string Endpoint { get; set; }
        public string EventsEndpoint { get; set; }
        public string AuthServiceURL { get; set; }
        public string StreamingServiceURL { get; set; }
        public string TelemetryServiceURL { get; set; }

        // Rates.
        /// <summary>
        /// Frequency for how often feature flags are fetched in polling mode, measured in seconds.
        /// Defaults to 60 Seconds if not set.
        /// </summary>
        public int? FeaturesRefreshRate { get; set; }

        /// <summary>
        /// Frequency for how often segments are fetched in polling mode, measured in seconds.
        /// Defaults to 60 Seconds if not set.
        /// </summary>
        public int? SegmentsRefreshRate { get; set; }

        /// <summary>
        /// Frequency for how often impressions flushed back to Harness servers, measured in seconds.
        /// Defaults to 30 Seconds if not set.
        /// </summary>
        public int? ImpressionsRefreshRate { get; set; }

        /// <summary>
        /// Frequency for how often track events flushed back to Harness servers, measured in seconds.
        /// Defaults to 60 Seconds if not set.
        /// </summary>
        public int? EventsPushRate { get; set; }

        [Obsolete]
        public int? MetricsRefreshRate { get; set; }

        /// <summary>
        /// Frequency for how often telemetry events flushed back to Harness servers, measured in seconds.
        /// Defaults to 3600 Seconds if not set.
        /// </summary>
        public int? TelemetryRefreshRate { get; set; }

        // Proxy
        /// <summary>
        /// Set the name of the proxy host.
        /// </summary>
        public string ProxyHost { get; set; }

        /// <summary>
        /// Set the port of the proxy.
        /// Defaults to 0 which indicate disabled.
        /// </summary>
        public int ProxyPort { get; set; }

        // Localhost
        /// <summary>
        /// Set path to localhost file for feature flags.
        /// </summary>
        public string LocalhostFilePath { get; set; }

        public ILocalhostFileSync LocalhostFileSync { get; set; }
    }
}
