﻿namespace Splitio.Constants
{
    public class Push
    {        
        public static string ControlPri => "control_pri";
        public static string ControlSec => "control_sec";
        public static string OccupancyPrefix => "[?occupancy=metrics.publishers]";
        public static int SecondsBeforeExpiration => 600; // how many seconds prior to token expiration to trigger reauth
    }

    public class Http
    {
        public static string Bearer => "Bearer";
        public static int ProtocolTypeTls12 => 3072;
        public static string SplitSDKVersion => "SplitSDKVersion";
        public static string SplitSDKImpressionsMode => "SplitSDKImpressionsMode";
        public static string SplitSDKMachineName => "SplitSDKMachineName";
        public static string SplitSDKMachineIP => "SplitSDKMachineIP";
        public static string SplitSDKClientKey => "SplitSDKClientKey";
        public static string Accept => "Accept";
        public static string AcceptEncoding => "Accept-Encoding";
        public static string KeepAlive => "Keep-Alive";
        public static string EventStream = "text/event-stream";
        public static string Gzip => "gzip";
    }

    public class Gral
    {
        public static string Unknown => "unknown";
        public static string NA => "NA";        
    }
}
