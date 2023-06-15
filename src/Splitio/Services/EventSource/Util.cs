using System;

namespace Splitio.Services.EventSource
{
    public static class Utils
    {
        public static NotificationStreamReader GetNotificationData(string line)
        {
            var array = line.Split('\n');
            var dataIndex = Array.FindIndex(array, row => row.Contains("data: "));
            var eventIndex = Array.FindIndex(array, row => row.Contains("event: "));

            if (dataIndex == -1 || eventIndex == -1) return null;

            return new NotificationStreamReader
            {
                Message = array[dataIndex].Replace("data: ", string.Empty),
                Type = array[eventIndex].Replace("event: ", string.Empty)
            };
        }
    }
}
