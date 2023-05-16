using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Util;
using System;
using System.Text;

namespace Splitio.Services.EventSource
{
    public class NotificationParser : INotificationParser
    {
        public IncomingNotification Parse(string notification)
        {
            if (notification.Contains("event: message"))
            {
                if (notification.Contains(Constants.Push.OccupancyPrefix))
                {
                    return ParseControlChannelMessage(notification);
                }

                return ParseMessage(notification);
            }
            else if (notification.Contains("event: error"))
            {
                return ParseError(notification);
            }

            return null;
        }

        private static IncomingNotification ParseMessage(string notificationString)
        {
            Console.WriteLine(notificationString);
            var notificationData = GetNotificationData<NotificationData>(notificationString);
            var data = JsonConvert.DeserializeObject<IncomingNotification>(notificationData.Data);

            IncomingNotification result;
            switch (data?.Type)
            {
                case NotificationType.SPLIT_UPDATE:
                    var changeNotification = JsonConvert.DeserializeObject<SplitChangeNotifiaction>(notificationData.Data);
                    result = DecompressSplitDefinition(changeNotification);
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

        private static IncomingNotification ParseControlChannelMessage(string notificationString)
        {
            var notificationData = GetNotificationData<NotificationData>(notificationString);
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
            var index = Array.FindIndex(notificationArray, row => row.Contains("data: "));

            return JsonConvert.DeserializeObject<T>(notificationArray[index].Replace("data: ", string.Empty));
        }

        private static IncomingNotification DecompressSplitDefinition(SplitChangeNotifiaction notification)
        {
            try
            {
                if (!notification.CompressionType.HasValue) return notification;

                var input = Convert.FromBase64String(notification.Data);

                switch (notification.CompressionType)
                {
                    case CompressionType.Gzip:
                        notification.Split = DeserializeSplitDefinitionObject(DecompressionUtil.GZip(input));
                        break;
                    case CompressionType.Zlib:
                        notification.Split = DeserializeSplitDefinitionObject(DecompressionUtil.ZLib(input));
                        break;
                    case CompressionType.NotCompressed:
                        notification.Split = DeserializeSplitDefinitionObject(input);
                        break;
                }
            }
            catch (Exception ex)
            {
                // log error.
            }

            return notification;
        }

        private static Split DeserializeSplitDefinitionObject(byte[] input)
        {
            return JsonConvert.DeserializeObject<Split>(Encoding.UTF8.GetString(input));
        }
    }
}
