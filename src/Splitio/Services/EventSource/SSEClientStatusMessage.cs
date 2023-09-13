namespace Splitio.Services.EventSource
{
    public enum SSEClientStatusMessage
    {
        INITIALIZATION_IN_PROGRESS,
        CONNECTED,
        FIRST_EVENT,
        RETRYABLE_ERROR,
        NONRETRYABLE_ERROR,
        FORCED_STOP
    }
}
