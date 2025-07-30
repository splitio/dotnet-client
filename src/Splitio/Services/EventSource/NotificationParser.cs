using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Util;
using System;
using System.Text;

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
            var data = JsonConvertWrapper.DeserializeObject<IncomingNotification>(notificationData.Data);

            IncomingNotification result;
            switch (data?.Type)
            {
                case NotificationType.SPLIT_UPDATE:
                    var sNotification = JsonConvertWrapper.DeserializeObject<SplitChangeNotification>(notificationData.Data);
                    sNotification.FeatureFlag = DecompressData<Split>(sNotification);
                    result = sNotification;
                    break;
                case NotificationType.RB_SEGMENT_UPDATE:
                    var rbNotification = JsonConvertWrapper.DeserializeObject<RuleBasedSegmentNotification>(notificationData.Data);
                    rbNotification.RuleBasedSegmentDto = DecompressData<RuleBasedSegmentDto>(rbNotification);
                    result = rbNotification;
                    break;
                case NotificationType.SPLIT_KILL:
                    result = JsonConvertWrapper.DeserializeObject<SplitKillNotification>(notificationData.Data);
                    break;
                case NotificationType.SEGMENT_UPDATE:
                    result = JsonConvertWrapper.DeserializeObject<SegmentChangeNotification>(notificationData.Data);
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
                var controlNotification = JsonConvertWrapper.DeserializeObject<ControlNotification>(notificationData.Data);
                controlNotification.Type = NotificationType.CONTROL;
                controlNotification.Channel = channel;

                return controlNotification;
            }

            return ParseOccupancy(notificationData.Data, channel);
        }

        private static IncomingNotification ParseOccupancy(string payload, string channel)
        {
            var occupancyNotification = JsonConvertWrapper.DeserializeObject<OccupancyNotification>(payload);

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

            return JsonConvertWrapper.DeserializeObject<T>(data.Trim());
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
            return JsonConvertWrapper.DeserializeObject<T>(Encoding.UTF8.GetString(input));
        }
    }
}
