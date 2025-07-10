using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Util;
using System;
using System.Text;
using Splitio.Common;
using Splitio.Services.Common;

namespace Splitio.Services.EventSource
{
    public class NotificationParser : INotificationParser
    {
        private static readonly string EventMessageType = "event: message";
        private static readonly string EventMessageWsType = "event:message";
        private static readonly string EventErrorType = "event: error";
        private static readonly string EventErrorWsType = "event:error";

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(NotificationParser));
        
        public IncomingNotification Parse(string notification)
        {
            try
            {
                if (notification.Contains(EventMessageType) || notification.Contains(EventMessageWsType))
                {
                    if (notification.Contains(Constants.Push.OccupancyPrefix))
                    {
                        return ParseControlChannelMessage(notification);
                    }

                    return ParseMessage(notification);
                }
                else if (notification.Contains(EventErrorType) || notification.Contains(EventErrorWsType))
                {
                    return ParseError(notification);
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Something went wrong parsing the notification: {notification}.", ex);
            }

            return null;
        }

        private IncomingNotification ParseMessage(string notificationString)
        {
            var notificationData = GetNotificationData<NotificationData>(notificationString);
            var data = JsonConvert.DeserializeObject<IncomingNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);

            IncomingNotification result;
            switch (data?.Type)
            {
                case NotificationType.SPLIT_UPDATE:
                    var sNotification = JsonConvert.DeserializeObject<SplitChangeNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);
                    sNotification.FeatureFlag = DecompressData<Split>(sNotification);
                    result = sNotification;
                    break;
                case NotificationType.RB_SEGMENT_UPDATE:
                    var rbNotification = JsonConvert.DeserializeObject<RuleBasedSegmentNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);
                    rbNotification.RuleBasedSegmentDto = DecompressData<RuleBasedSegmentDto>(rbNotification);
                    result = rbNotification;
                    break;
                case NotificationType.SPLIT_KILL:
                    result = JsonConvert.DeserializeObject<SplitKillNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);
                    break;
                case NotificationType.SEGMENT_UPDATE:
                    result = JsonConvert.DeserializeObject<SegmentChangeNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);
                    break;
                default:
                    return null;
            }

            result.Channel = notificationData.Channel;

            return result;
        }

        private static IncomingNotification ParseControlChannelMessage(string notificationString)
        {
            var notificationData = GetNotificationData<NotificationData>(notificationString);
            var channel = notificationData.Channel.Replace(Constants.Push.OccupancyPrefix, string.Empty);

            if (notificationData.Data.Contains("controlType"))
            {
                var controlNotification = JsonConvert.DeserializeObject<ControlNotification>(notificationData.Data, SerializerSettings.DefaultSerializerSettings);
                controlNotification.Type = NotificationType.CONTROL;
                controlNotification.Channel = channel;

                return controlNotification;
            }

            return ParseOccupancy(notificationData.Data, channel);
        }

        private static IncomingNotification ParseOccupancy(string payload, string channel)
        {
            var occupancyNotification = JsonConvert.DeserializeObject<OccupancyNotification>(payload, SerializerSettings.DefaultSerializerSettings);

            if (occupancyNotification?.Metrics == null)
                return null;

            occupancyNotification.Type = NotificationType.OCCUPANCY;
            occupancyNotification.Channel = channel;

            return occupancyNotification;
        }

        private static IncomingNotification ParseError(string notificationString)
        {
            var notificatinError = GetNotificationData<NotificationError>(notificationString);

            if (notificatinError.Message == null)
                return null;

            notificatinError.Type = NotificationType.ERROR;

            return notificatinError;
        }

        private static T GetNotificationData<T>(string notificationString)
        {
            var notificationArray = notificationString.Split('\n');
            var index = Array.FindIndex(notificationArray, row => row.Contains("data:"));
            var data = notificationArray[index].Replace("data:", string.Empty);

            return JsonConvert.DeserializeObject<T>(data.Trim(), SerializerSettings.DefaultSerializerSettings);
        }

        private T DecompressData<T>(InstantUpdateNotification notification) where T : class
        {
            if (!notification.CompressionType.HasValue)
                return null;

            try
            {
                var input = Convert.FromBase64String(notification.Data);

                switch (notification.CompressionType)
                {
                    case CompressionType.Gzip:
                        return DeserializeObject<T>(DecompressionUtil.GZip(input));
                    case CompressionType.Zlib:
                        return DeserializeObject<T>(DecompressionUtil.ZLib(input));
                    case CompressionType.NotCompressed:
                        return DeserializeObject<T>(input);
                    default:
                        _log.Debug($"Unsupported compression type: {notification.CompressionType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Somenthing went wrong decompressing message data.", ex);
            }

            return null;
        }

        private static T DeserializeObject<T>(byte[] input)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(input), SerializerSettings.DefaultSerializerSettings);
        }
    }
}
