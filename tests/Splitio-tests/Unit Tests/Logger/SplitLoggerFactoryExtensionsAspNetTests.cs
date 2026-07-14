#if NETCORE
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Splitio_Tests.Unit_Tests.Logger
{
    [TestClass]
    public class SplitLoggerFactoryExtensionsAspNetTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            // Reset static state between tests
            var emptyFactory = LoggerFactory.Create(builder => { });
            Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.AddSplitLogs(emptyFactory);
        }

        [TestMethod]
        public void AddSplitLogs_WithHostBuilder_ShouldSetLoggerFactoryBeforeHostRuns()
        {
            // Arrange - Simulate ASP.NET Core Host Builder pattern
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSplitLogs();
                });

            // Act - Build the host (this should trigger DI container creation)
            using var host = hostBuilder.Build();

            // Assert - Check if logger factory is set BEFORE calling host.Run()
            // This is critical because logging might be used during startup
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;
            var factory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            // The current implementation may fail this because the hosted service
            // factory is only called when resolving IHostedService instances
            Assert.IsNotNull(factory, "Logger factory should be set after Build() but before Run()");
            Assert.IsTrue(hasValue, "LoggerFactoryHasValue should be true after Build()");
        }

        [TestMethod]
        public void AddSplitLogs_WithHostBuilder_LoggerFactoryAvailableAfterStart()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSplitLogs();
                });

            // Act - Build and start the host
            using var host = hostBuilder.Build();

            // Force hosted services to be resolved
            var hostedServices = host.Services.GetServices<IHostedService>();
            foreach (var _ in hostedServices) { } // Force enumeration

            // Assert
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;
            var factory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            Assert.IsNotNull(factory, "Logger factory should be set after hosted services are resolved");
            Assert.IsTrue(hasValue);
        }

        [TestMethod]
        public void AddSplitLogs_WithServiceProvider_LoggerFactoryAvailableImmediately()
        {
            // Arrange - Test eager initialization approach
            var services = new ServiceCollection();
            services.AddLogging(logging =>
            {
                logging.AddSplitLogs();
            });

            // Act - Build service provider
            using var serviceProvider = services.BuildServiceProvider();

            // Force hosted service to be registered by resolving it
            var hostedService = serviceProvider.GetServices<IHostedService>();
            var _ = hostedService.GetEnumerator(); // Start enumeration

            // Assert - This might not work until hosted services are fully resolved
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;

            // If this fails, it means the timing issue exists
            Assert.IsTrue(hasValue, "Logger factory should be available after service resolution");
        }

        [TestMethod]
        public void AddSplitLogs_ManualInitialization_WorksImmediately()
        {
            // Arrange - Alternative approach: manual initialization
            var services = new ServiceCollection();
            services.AddLogging();

            using var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            // Act - Manually call AddSplitLogs on the factory
            loggerFactory.AddSplitLogs();

            // Assert
            var hasValue = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.LoggerFactoryHasValue;
            var retrievedFactory = Microsoft.Extensions.Logging.SplitLoggerFactoryExtensions.GetLoggerFactory();

            Assert.IsTrue(hasValue);
            Assert.AreSame(loggerFactory, retrievedFactory);
        }
    }
}
#endif
