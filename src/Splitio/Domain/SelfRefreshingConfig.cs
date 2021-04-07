namespace Splitio.Domain
{
    public class SelfRefreshingConfig : BaseConfig
    {        
        public long HttpConnectionTimeout { get; set; }
        public long HttpReadTimeout { get; set; }
        public int ConcurrencyLevel { get; set; }        
        public int TreatmentLogSize { get; set; }
        public int EventsFirstPushWindow { get; set; }
        public int EventLogSize { get; set; }
        public int NumberOfParalellSegmentTasks { get; set; }
        public bool RandomizeRefreshRates { get; set; }
        public bool StreamingEnabled { get; set; }
        public int AuthRetryBackoffBase { get; set; }
        public int StreamingReconnectBackoffBase { get; set; }
        
        public ImpressionsMode ImpressionsMode { get; set; }

        // Urls.
        public string BaseUrl { get; set; }
        public string EventsBaseUrl { get; set; }
        public string AuthServiceURL { get; set; }
        public string StreamingServiceURL { get; set; }
        public string TelemetryServiceURL { get; set; }

        // Rates.
        public int SplitsRefreshRate { get; set; }
        public int SegmentRefreshRate { get; set; }
        public int EventLogRefreshRate { get; set; }
        public int TreatmentLogRefreshRate { get; set; }
        public int TelemetryRefreshRate { get; set; }
    }
}
