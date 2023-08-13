using System;

namespace Splitio.Services.EventSource
{
    public class ReadStreamException : Exception
    {
        public SSEClientStatusMessage Status { get; set; }

        public ReadStreamException(SSEClientStatusMessage status, string message)
            : base(message)
        {
            Status = status;
        }
    }
}
