namespace Splitio.Services.EventSource
{
    public class StreamingStatusEventArgs
    {
        public StreamingStatus Status { get; }

        public StreamingStatusEventArgs(StreamingStatus status)
        {
            Status = status;
        }
    }
}
