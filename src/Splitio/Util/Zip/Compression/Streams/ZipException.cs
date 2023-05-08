using System;
using System.Runtime.Serialization;

namespace Splitio.Util.Zip.Compression.Streams
{
    [Serializable]
    internal class ZipException : Exception
    {
        public ZipException()
        {
        }

        public ZipException(string message) : base(message)
        {
        }

        public ZipException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZipException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}