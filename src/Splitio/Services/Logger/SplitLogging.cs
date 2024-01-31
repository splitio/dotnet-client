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
            if (_level <= Level.Debug)
            {
                WriteLog(Level.Debug, message, exception);
            }
        }

        public void Debug(string message)
        {
            if (_level <= Level.Debug)
            {
                WriteLog(Level.Debug, message);
            }
        }

        public void Error(string message, Exception exception)
        {
            if (_level <= Level.Error)
            {
                WriteLog(Level.Error, message, exception);
            }
        }

        public void Error(string message)
        {
            if (_level <= Level.Error)
            {
                WriteLog(Level.Error, message);
            }
        }

        public void Info(string message, Exception exception)
        {
            if (_level <= Level.Info)
            {
                WriteLog(Level.Info, message, exception);
            }
        }

        public void Info(string message)
        {
            if (_level <= Level.Info)
            {
                WriteLog(Level.Info, message);
            }
        }

        public void Trace(string message, Exception exception)
        {
            if (_level <= Level.Trace)
            {
                WriteLog(Level.Trace, message, exception);
            }
        }

        public void Trace(string message)
        {
            if (_level <= Level.Trace)
            {
                WriteLog(Level.Trace, message);
            }
        }

        public void Warn(string message, Exception exception)
        {
            if (_level <= Level.Warn)
            {
                WriteLog(Level.Warn, message, exception);
            }
        }

        public void Warn(string message)
        {
            if (_level <= Level.Warn)
            {
                WriteLog(Level.Warn, message);
            }
        }

        private void WriteLog(Level level, string message, Exception exception = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString()).Append(" [").Append(level).Append("] ").Append(message).Append(exception);

            _textWriter.WriteLine(sb.ToString());
        }
    }
}
