using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Integration_tests.Async
{
    [TestClass]
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
                EventsPushRate = 1
            };
        }

        protected override async Task AssertSentImpressionsAsync(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            await InMemoryHelper.AssertSentImpressionsAsync(sentImpressionsCount, httpClientMock, expectedImpressions);
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExcpected, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true)
        {
            await InMemoryHelper.AssertSentEventsAsync(eventsExcpected, httpClientMock, sleepTime, eventsCount, validateEvents);
        }

        protected override async Task CleanupAsync()
        {
            httpClientMock.ResetLogEntries();

            await Task.FromResult(0);
        }

        protected override async Task DelayAsync()
        {
            await Task.Delay(500);
        }
    }
}
