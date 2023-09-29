using System;

namespace Splitio.Services.Logger
{
    public class NoopLogging : ISplitLogger
    {
        public bool IsDebugEnabled => false;

        public void Debug(string message, Exception exception)
        {
            // No-op
        }

        public void Debug(string message)
        {
            // No-op
        }

        public void Error(string message, Exception exception)
        {
            // No-op
        }

        public void Error(string message)
        {
            // No-op
        }

        public void Info(string message, Exception exception)
        {
            // No-op
        }

        public void Info(string message)
        {
            // No-op
        }

        public void Trace(string message, Exception exception)
        {
            // No-op
        }

        public void Trace(string message)
        {
            // No-op
        }

        public void Warn(string message, Exception exception)
        {
            // No-op
        }

        public void Warn(string message)
        {
            // No-op
        }
    }
}
