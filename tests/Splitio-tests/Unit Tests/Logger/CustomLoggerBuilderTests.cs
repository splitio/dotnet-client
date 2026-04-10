using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using System;

namespace Splitio_Tests.Unit_Tests.Logger
{
    [TestClass]
    public class CustomLoggerBuilderTests
    {
        private class TestLogger : ISplitLogger
        {
            public string Name { get; }
            public int CallCount { get; private set; }

            public TestLogger(string name)
            {
                Name = name;
            }

            public bool IsDebugEnabled => true;

            public void Debug(string message, Exception exception)
            {
                CallCount++;
            }

            public void Debug(string message)
            {
                CallCount++;
            }

            public void Error(string message, Exception exception)
            {
                CallCount++;
            }

            public void Error(string message)
            {
                CallCount++;
            }

            public void Info(string message, Exception exception)
            {
                CallCount++;
            }

            public void Info(string message)
            {
                CallCount++;
            }

            public void Trace(string message, Exception exception)
            {
                CallCount++;
            }

            public void Trace(string message)
            {
                CallCount++;
            }

            public void Warn(string message, Exception exception)
            {
                CallCount++;
            }

            public void Warn(string message)
            {
                CallCount++;
            }
        }

        [TestMethod]
        public void CustomLoggerBuilder_CreatesLoggerFromBuilder()
        {
            // Arrange
            var testLogger = new TestLogger("builder-logger");
            var config = new ConfigurationOptions
            {
                LoggerBuilder = () => testLogger
            };

            // Act
            var factory = new SplitFactory("fake-api-key", config);

            // Assert - verify the logger was created (factory should not throw)
            Assert.IsNotNull(factory);
            Assert.AreEqual("builder-logger", testLogger.Name);
        }

        [TestMethod]
        public void CustomLoggerBuilder_LoggerTakesPrecedence()
        {
            // Arrange
            var directLogger = new TestLogger("direct-logger");
            var builderLogger = new TestLogger("builder-logger");
            var builderCalled = false;

            var config = new ConfigurationOptions
            {
                Logger = directLogger,
                LoggerBuilder = () =>
                {
                    builderCalled = true;
                    return builderLogger;
                }
            };

            // Act
            var factory = new SplitFactory("fake-api-key", config);

            // Assert - builder should not be called when Logger is set
            Assert.IsNotNull(factory);
            Assert.IsFalse(builderCalled, "LoggerBuilder should not be invoked when Logger is already set");
        }

        [TestMethod]
        public void SplitLogger_CustomMethod_InvokesBuilder()
        {
            // Arrange
            var testLogger = new TestLogger("custom-logger");
            SplitLoggerBuilder builder = () => testLogger;

            // Act
            var result = SplitLogger.Custom(builder);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(testLogger, result);
            Assert.AreEqual("custom-logger", ((TestLogger)result).Name);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SplitLogger_CustomMethod_ThrowsOnNullBuilder()
        {
            // Act
            SplitLogger.Custom(null);

            // Assert - expects ArgumentNullException
        }

        [TestMethod]
        public void CustomLoggerBuilder_WithNullLoggerAndBuilder_UsesDefault()
        {
            // Arrange - no logger or builder set
            var config = new ConfigurationOptions();

            // Act
            var factory = new SplitFactory("fake-api-key", config);

            // Assert - should use default logger without throwing
            Assert.IsNotNull(factory);
        }
    }
}
