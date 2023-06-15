using Newtonsoft.Json;
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
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(NotificationParser));
        private static readonly string EventMessageType = "message";
        private static readonly string EventErrorType = "error";

        #region Public Methods
        public IncomingNotification Parse(NotificationStreamReader notification)
        {
            if (notification.Type == EventMessageType)
            {
                if (notification.Message.Contains(Constants.Push.OccupancyPrefix))
                {
                    return ParseControlChannelMessage(notification);
                }

                return ParseMessage(notification);
            }
            else if (notification.Type == EventErrorType)
            {
                return ParseError(notification);
            }

            return null;
        }

        public static NotificationStreamReader GetNotificationData(string line)
        {
            var array = line.Split('\n');
            var dataIndex = Array.FindIndex(array, row => row.Contains("data: "));
            var eventIndex = Array.FindIndex(array, row => row.Contains("event: "));

            return new NotificationStreamReader
            {
                Message = array[dataIndex].Replace("data: ", string.Empty),
                Type = array[eventIndex].Replace("event: ", string.Empty)
            };
        }
        #endregion

        #region Private Methods
        private static IncomingNotification ParseMessage(NotificationStreamReader notification)
        {
            var notificationData = JsonConvert.DeserializeObject<NotificationData>(notification.Message);
            var data = JsonConvert.DeserializeObject<IncomingNotification>(notificationData.Data);

            IncomingNotification result;
            switch (data?.Type)
            {
                case NotificationType.SPLIT_UPDATE:
                    var changeNotification = JsonConvert.DeserializeObject<SplitChangeNotification>(notificationData.Data);
                    result = DecompressData(changeNotification);
                    break;
                case NotificationType.SPLIT_KILL:
                    result = JsonConvert.DeserializeObject<SplitKillNotification>(notificationData.Data);
                    break;
                case NotificationType.SEGMENT_UPDATE:
                    result = JsonConvert.DeserializeObject<SegmentChangeNotification>(notificationData.Data);
                    break;
                default:
                    return null;
            }

            result.Channel = notificationData.Channel;

            return result;
        }

        private static IncomingNotification ParseControlChannelMessage(NotificationStreamReader notification)
        {
            var notificationData = JsonConvert.DeserializeObject<NotificationData>(notification.Message);
            var channel = notificationData.Channel.Replace(Constants.Push.OccupancyPrefix, string.Empty);

            if (notificationData.Data.Contains("controlType"))
            {
                var controlNotification = JsonConvert.DeserializeObject<ControlNotification>(notificationData.Data);
                controlNotification.Type = NotificationType.CONTROL;
                controlNotification.Channel = channel;

                return controlNotification;
            }

            return ParseOccupancy(notificationData.Data, channel);
        }

        private static IncomingNotification ParseOccupancy(string payload, string channel)
        {
            var occupancyNotification = JsonConvert.DeserializeObject<OccupancyNotification>(payload);

            if (occupancyNotification?.Metrics == null)
                return null;

            occupancyNotification.Type = NotificationType.OCCUPANCY;
            occupancyNotification.Channel = channel;

            return occupancyNotification;
        }

        private static IncomingNotification ParseError(NotificationStreamReader notification)
        {
            var notificatinError = JsonConvert.DeserializeObject<NotificationError>(notification.Message);

            if (notificatinError.Message == null)
                return null;

            notificatinError.Type = NotificationType.ERROR;

            return notificatinError;
        }

        private static IncomingNotification DecompressData(SplitChangeNotification notification)
        {
            try
            {
                if (!notification.CompressionType.HasValue) return notification;

                var input = Convert.FromBase64String(notification.Data);

                switch (notification.CompressionType)
                {
                    case CompressionType.Gzip:
                        notification.FeatureFlag = DeserializeSplitObject(DecompressionUtil.GZip(input));
                        break;
                    case CompressionType.Zlib:
                        notification.FeatureFlag = DeserializeSplitObject(DecompressionUtil.ZLib(input));
                        break;
                    case CompressionType.NotCompressed:
                        notification.FeatureFlag = DeserializeSplitObject(input);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Somenthing went wrong decompressing message data.", ex);
            }

            return notification;
        }

        private static Split DeserializeSplitObject(byte[] input)
        {
            return JsonConvert.DeserializeObject<Split>(Encoding.UTF8.GetString(input));
        }
        #endregion
    }
}
