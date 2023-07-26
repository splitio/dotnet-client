using System;
using System.IO;
using System.Text;

namespace Splitio.Services.Logger
{
    public class SplitLogging : ISplitLogger
    {
        private readonly Level _level;
        private readonly TextWriter _textWriter;

        public SplitLogging(Level level, TextWriter textWriter)
        {
            _level = level;
            _textWriter = textWriter;
        }

        public bool IsDebugEnabled => true;

        public void Debug(string message, Exception exception)
        {
            if (_level == Level.Debug)
            {
                WriteLog(message, exception);
            }
        }

        public void Debug(string message)
        {
            if (_level == Level.Debug)
            {
                WriteLog(message);
            }
        }

        public void Error(string message, Exception exception)
        {
            if (_level == Level.Error)
            {
                WriteLog(message, exception);
            }
        }

        public void Error(string message)
        {
            if (_level == Level.Error)
            {
                WriteLog(message);
            }
        }

        public void Info(string message, Exception exception)
        {
            if (_level >= Level.Info)
            {
                WriteLog(message, exception);
            }
        }

        public void Info(string message)
        {
            if (_level >= Level.Info)
            {
                WriteLog(message);
            }
        }

        public void Trace(string message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void Trace(string message)
        {
            throw new NotImplementedException();
        }

        public void Warn(string message, Exception exception)
        {
            if (_level >= Level.Warn)
            {
                WriteLog(message, exception);
            }
        }

        public void Warn(string message)
        {
            if (_level >= Level.Warn)
            {
                WriteLog(message);
            }
        }

        private void WriteLog(string message, Exception exception = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString()).Append(" [").Append(_level.ToString()).Append("] ").Append(message).Append(exception);

            _textWriter.WriteLine(sb.ToString());
        }
    }
}
