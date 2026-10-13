using System;

namespace Splitio.Commons.Engine.Matchers.SemverImp
{
    public class SemverParseException : Exception
    {
        public SemverParseException(string message) : base(message) { }
    }
}
