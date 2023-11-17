using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Integration_tests.Resources
{
    public class Helper
    {
        public static void AssertImpression(KeyImpression impression, long changeNumber, string feature, string keyName, string label, string treatment)
        {
            Assert.AreEqual(changeNumber, impression.changeNumber);
            Assert.AreEqual(feature, impression.feature);
            Assert.AreEqual(keyName, impression.keyName);
            Assert.AreEqual(label, impression.label);
            Assert.AreEqual(treatment, impression.treatment);
        }
    }

    public class InMemoryHelper
    {
        public static async Task AssertSentImpressionsAsync(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            if (sentImpressionsCount <= 0) return;

            var sentImpressions = new List<KeyImpressionBackend>();

            for (int i = 0; i < 3; i++)
            {
                sentImpressions = GetImpressionsSentBackend(httpClientMock);

                if (sentImpressions.Count > 0) break;

                await Task.Delay(1000);
            }

            Assert.AreEqual(sentImpressionsCount, sentImpressions.Sum(si => si.I.Count), "AssertSentImpressions");

            foreach (var expectedImp in expectedImpressions)
            {
                var impressions = new List<ImpressionData>();

                foreach (var ki in sentImpressions.Where(si => si.F.Equals(expectedImp.feature)))
                {
                    impressions.AddRange(ki.I);
                }

                Assert.IsTrue(impressions
                    .Where(si => expectedImp.bucketingKey == si.B)
                    .Where(si => expectedImp.changeNumber == si.C)
                    .Where(si => expectedImp.keyName == si.K)
                    .Where(si => expectedImp.label == si.R)
                    .Where(si => expectedImp.treatment == si.T)
                    .Any());
            }
        }

        public static List<KeyImpressionBackend> GetImpressionsSentBackend(HttpClientMock httpClientMock = null)
        {
            var impressions = new List<KeyImpressionBackend>();
            var logs = httpClientMock.GetImpressionLogs();

            foreach (var log in logs)
            {
                var _impressions = JsonConvert.DeserializeObject<List<KeyImpressionBackend>>(log.RequestMessage.Body);

                impressions.AddRange(_impressions);
            }

            return impressions;
        }

        public static async Task AssertSentEventsAsync(List<EventBackend> eventsExpected, HttpClientMock httpClientMock = null, int sleepTime = 5000, int? eventsCount = null, bool validateEvents = true)
        {
            await Task.Delay(sleepTime);

            var sentEvents = GetEventsSentBackend(httpClientMock);

            Assert.AreEqual(eventsCount ?? eventsExpected.Count, sentEvents.Count);

            if (validateEvents)
            {
                foreach (var expected in eventsExpected)
                {
                    Assert.IsTrue(sentEvents
                        .Where(ee => ee.Key == expected.Key)
                        .Where(ee => ee.EventTypeId == expected.EventTypeId)
                        .Where(ee => ee.Value == expected.Value)
                        .Where(ee => ee.TrafficTypeName == expected.TrafficTypeName)
                        .Where(ee => ee.Properties?.Count == expected.Properties?.Count)
                        .Any());
                }
            }
        }

        public static List<EventBackend> GetEventsSentBackend(HttpClientMock httpClientMock = null)
        {
            var events = new List<EventBackend>();
            var logs = httpClientMock.GetEventsLog();

            foreach (var log in logs)
            {
                var _events = JsonConvert.DeserializeObject<List<EventBackend>>(log.RequestMessage.Body);

                foreach (var item in _events)
                {
                    events.Add(item);
                }
            }

            return events;
        }
    }
}
