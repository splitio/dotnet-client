namespace Splitio.Constants
{
    public static class Push
    {        
        public static string ControlPri => "control_pri";
        public static string ControlSec => "control_sec";
        public static string OccupancyPrefix => "[?occupancy=metrics.publishers]";
        public static int SecondsBeforeExpiration => 600; // how many seconds prior to token expiration to trigger reauth
    }

    public static class Http
    {
        public static string Bearer => "Bearer";
        public static string SplitSDKVersion => "SplitSDKVersion";
        public static string SplitSDKImpressionsMode => "SplitSDKImpressionsMode";
        public static string SplitSDKMachineName => "SplitSDKMachineName";
        public static string SplitSDKMachineIP => "SplitSDKMachineIP";
        public static string SplitSDKClientKey => "SplitSDKClientKey";
        public static string Accept => "Accept";
        public static string AcceptEncoding => "Accept-Encoding";
        public static string KeepAlive => "Keep-Alive";
        public static string Gzip => "gzip";
        public static string CacheControlKey => "Cache-Control";
        public static string CacheControlValue => "no-cache";
        public static string MediaTypeJson => "application/json";
    }

    public static class Gral
    {
        public static string Unknown => "unknown";
        public static string NA => "NA";
        public static int DestroyTimeount => 30000;
        public static int IntervalToClearLongTermCache => 3600000;
        public static string Control => "control";
    }

    public static class Urls
    {
        public static string BaseUrl => "https://sdk.split.io";
        public static string EventsBaseUrl => "https://events.split.io";
        public static string AuthServiceURL => "https://auth.split.io/api/auth";
        public static string StreamingServiceURL => "https://streaming.split.io/sse";
        public static string TelemetryServiceURL => "https://telemetry.split.io/api/v1";
    }

    public static class StorageType
    {
        public static string Memory => "memory";
        public static string Redis => "redis";
    }

    public static class Messages
    {
        public static string InitDestroy => "Initialitation sdk destroy.";
        public static string Destroyed => "SDK has been destroyed.";
    }
}
