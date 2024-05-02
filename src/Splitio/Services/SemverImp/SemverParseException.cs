using System;

namespace Splitio.Services.SemverImp
{
    public class SemverParseException : Exception
    {
        public SemverParseException(string message) : base(message) { }
    }
}
