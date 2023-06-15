namespace Splitio.Services.EventSource
{
    public interface INotificationParser
    {
        IncomingNotification Parse(NotificationStreamReader notification);
    }
}
