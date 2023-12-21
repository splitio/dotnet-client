using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Tests.Common;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Integration_tests
{
    [TestClass, TestCategory("Integration")]
    public class InMemoryClientAsyncTests : BaseAsyncClientTests
    {
        private static readonly HttpClientMock httpClientMock = new HttpClientMock("async");

        public InMemoryClientAsyncTests() : base("InMemory")
        {
        }

        protected override ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            return new ConfigurationOptions
            {
                Endpoint = httpClientMock.GetUrl(),
                EventsEndpoint = httpClientMock.GetUrl(),
                TelemetryServiceURL = httpClientMock.GetUrl(),
                ImpressionListener = impressionListener,
                EventsPushRate = 1,
                Logger = SplitLogger.Console(Level.Debug)
            };
        }

        protected override void AssertSentImpressions(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            InMemoryHelper.AssertSentImpressions(sentImpressionsCount, httpClientMock, expectedImpressions);
        }

        protected override void AssertSentEvents(List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true)
        {
            InMemoryHelper.AssertSentEvents(eventsExcpected, httpClientMock, eventsCount, validateEvents);
        }

        protected override async Task CleanupAsync()
        {
            httpClientMock.ResetLogEntries();

            await Task.FromResult(0);
        }
    }
}
