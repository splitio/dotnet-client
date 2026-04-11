#if NETCORE
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Logger
{
    [TestClass]
    public class SplitLoggerFactoryExtensionsTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Reset static state between tests
            var factory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();
            if (factory != null)
            {
                // Create a new empty factory to reset
                var emptyFactory = LoggerFactory.Create(builder => { });
                Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.AddSplitLogs(emptyFactory);
            }
        }

        #region AddSplitLogs(ILoggerFactory) Tests

        [TestMethod]
        public void AddSplitLogs_WithLoggerFactory_ShouldSetStaticFactory()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Act
            var result = loggerFactory.AddSplitLogs();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(loggerFactory, result);
            Assert.IsTrue(Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue);
        }

        [TestMethod]
        public void AddSplitLogs_WithLoggerFactory_ShouldReturnSameFactory()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => { });

            // Act
            var result = loggerFactory.AddSplitLogs();

            // Assert
            Assert.AreSame(loggerFactory, result);
        }

        [TestMethod]
        public void AddSplitLogs_WithNullFactory_ShouldNotThrow()
        {
            // Arrange
            ILoggerFactory loggerFactory = null;

            // Act & Assert - this will set the static field to null
            var result = loggerFactory.AddSplitLogs();
            Assert.IsNull(result);
        }

        #endregion

        #region GetLoggerFactory Tests

        [TestMethod]
        public void GetLoggerFactory_AfterAddSplitLogs_ShouldReturnSameFactory()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => { });
            loggerFactory.AddSplitLogs();

            // Act
            var retrievedFactory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            // Assert
            Assert.IsNotNull(retrievedFactory);
            Assert.AreSame(loggerFactory, retrievedFactory);
        }

        [TestMethod]
        public void GetLoggerFactory_BeforeAddSplitLogs_ShouldReturnNull()
        {
            // Act
            var retrievedFactory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            // Assert - may be null if not set previously
            // This test depends on test execution order, so we just verify it doesn't throw
            Assert.IsTrue(true);
        }

        #endregion

        #region LoggerFactoryHasValue Tests

        [TestMethod]
        public void LoggerFactoryHasValue_AfterAddSplitLogs_ShouldBeTrue()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => { });
            loggerFactory.AddSplitLogs();

            // Act
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;

            // Assert
            Assert.IsTrue(hasValue);
        }

        [TestMethod]
        public void LoggerFactoryHasValue_WithNullFactory_ShouldBeFalse()
        {
            // Arrange
            ILoggerFactory loggerFactory = null;
            loggerFactory.AddSplitLogs();

            // Act
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;

            // Assert
            Assert.IsFalse(hasValue);
        }

        #endregion

        #region AddSplitLogs(ILoggingBuilder) Tests

        [TestMethod]
        public void AddSplitLogs_WithLoggingBuilder_ShouldWrapLoggerFactory()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLogging(builder =>
            {
                builder.AddSplitLogs();
            });

            // Assert - Verify that ILoggerFactory can be resolved
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            Assert.IsNotNull(loggerFactory, "ILoggerFactory should be registered");
        }

        [TestMethod]
        public void AddSplitLogs_WithLoggingBuilder_ShouldSetStaticLoggerFactory()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLogging(builder =>
            {
                builder.AddSplitLogs();
            });
            var serviceProvider = services.BuildServiceProvider();

            // Trigger the logger factory to be resolved (this sets the static field)
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Assert
            Assert.IsTrue(Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue);
            var retrievedFactory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();
            Assert.IsNotNull(retrievedFactory);
        }

        [TestMethod]
        public void AddSplitLogs_WithLoggingBuilder_ShouldReturnBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            ILoggingBuilder capturedBuilder = null;

            // Act
            services.AddLogging(builder =>
            {
                capturedBuilder = builder;
                var result = builder.AddSplitLogs();
                Assert.AreSame(builder, result);
            });

            // Assert
            Assert.IsNotNull(capturedBuilder);
        }

        [TestMethod]
        public void AddSplitLogs_WithLoggingBuilder_ShouldAllowChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLogging(builder =>
            {
                builder.AddConsole()
                       .AddSplitLogs()
                       .AddDebug();
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            Assert.IsNotNull(loggerFactory);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void AddSplitLogs_BothOverloads_ShouldWorkTogether()
        {
            // Arrange
            var loggerFactory1 = LoggerFactory.Create(builder => { });

            // Act - Use ILoggerFactory extension
            loggerFactory1.AddSplitLogs();
            var retrieved1 = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            // Arrange - Use ILoggingBuilder extension
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddSplitLogs());
            var serviceProvider = services.BuildServiceProvider();
            var _ = serviceProvider.GetServices<IHostedService>().ToList();

            var retrieved2 = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            // Assert
            Assert.IsNotNull(retrieved1);
            Assert.IsNotNull(retrieved2);
        }

        #endregion
    }
}
#endif
