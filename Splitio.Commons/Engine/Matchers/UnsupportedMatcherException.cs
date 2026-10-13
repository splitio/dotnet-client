using System;

namespace Splitio.Commons.Engine.Matchers
{
    public class UnsupportedMatcherException : Exception
    {
        public UnsupportedMatcherException(string message) : base(message) { }
    }
}
