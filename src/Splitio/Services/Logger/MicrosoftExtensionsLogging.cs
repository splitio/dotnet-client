#if NET_LATEST
using Microsoft.Extensions.Logging;
using System;

namespace Splitio.Services.Logger
{
    public class MicrosoftExtensionsLogging : ISplitLogger
    {
        private readonly ILogger _logger;

        public MicrosoftExtensionsLogging(ILoggerFactory loggerFactory, string type)
        {
            _logger = loggerFactory.CreateLogger(type);
        }
        public MicrosoftExtensionsLogging(Type type)
        {
            var loggerFactory = SplitLoggerFactoryExtensions.GetLoggerFactory();
            _logger = loggerFactory.CreateLogger(type);
        }

        public MicrosoftExtensionsLogging(string type)
        {
            var loggerFactory = SplitLoggerFactoryExtensions.GetLoggerFactory();
            _logger = loggerFactory.CreateLogger(type);
        }

        public void Debug(string message, Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(exception, message);
        }

        public void Debug(string message)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(message);
        }

        public void Error(string message, Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(exception, message);
        }

        public void Error(string message)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError(message);
        }        

        public void Info(string message, Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation(exception, message);
        }

        public void Info(string message)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation(message);
        }
        
        public void Trace(string message, Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(exception, message);
        }

        public void Trace(string message)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(message);
        }

        public void Warn(string message, Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(exception, message);
        }

        public void Warn(string message)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(message);
        }

        public bool IsDebugEnabled => _logger.IsEnabled(LogLevel.Debug);
    }
}
#endif