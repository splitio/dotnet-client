using System;

namespace Splitio.Services.Parsing.Classes
{
    internal class UnsupportedMatcherException : Exception
    {
        public UnsupportedMatcherException(string message) : base(message) { }
    }
}
