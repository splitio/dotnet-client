﻿using Newtonsoft.Json;
using Splitio.Domain;

namespace Splitio.Services.EventSource
{
    public class Notification
    {
        public string Id { get; set; }
        public string Event { get; set; }
        public NotificationData Data { get; set; }
    }

    public class NotificationData
    {
        public string Id { get; set; }
        public string Channel { get; set; }
        public string Data { get; set; }
    }       

    public enum NotificationType
    {
        SPLIT_UPDATE,
        SPLIT_KILL,
        SEGMENT_UPDATE,
        RB_SEGMENT_UPDATE,
        CONTROL,
        OCCUPANCY,
        ERROR
    }

    public enum ControlType
    {
        STREAMING_PAUSED,
        STREAMING_RESUMED,
        STREAMING_DISABLED
    }

    public class IncomingNotification
    {
        public NotificationType Type { get; set; }
        public string Channel { get; set; }
    }

    public class NotificationError : IncomingNotification
    {
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public int Code { get; set; }
    }

    public class InstantUpdateNotification : IncomingNotification
    {
        public long ChangeNumber { get; set; }
        [JsonProperty("pcn")]
        public long? PreviousChangeNumber { get; set; }
        [JsonProperty("d")]
        public string Data { get; set; }
        [JsonProperty("c")]
        public CompressionType? CompressionType { get; set; }

        public override string ToString()
        {
            return $"cn: {ChangeNumber} - pcn: {PreviousChangeNumber} - c: {CompressionType}";
        }
    }

    public class SplitChangeNotification : InstantUpdateNotification
    {
        public Split FeatureFlag { get; set; }
    }
    public class RuleBasedSegmentNotification : InstantUpdateNotification
    {
        public RuleBasedSegmentDto RuleBasedSegmentDto { get; set; }
    }

    public class SplitKillNotification : IncomingNotification
    {
        public long ChangeNumber { get; set; }
        public string DefaultTreatment { get; set; }
        public string SplitName { get; set; }
    }

    public class SegmentChangeNotification : IncomingNotification
    {
        public long ChangeNumber { get; set; }
        public string SegmentName { get; set; }
    }

    public class ControlNotification : IncomingNotification
    {
        public ControlType ControlType { get; set; }
    }

    public class OccupancyNotification : IncomingNotification
    {
        public OccupancyMetricsData Metrics { get; set; }
    }

    public class OccupancyMetricsData
    {
        public int Publishers { get; set; }
    }
}
