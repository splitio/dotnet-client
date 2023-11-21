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
        private readonly static List<KeyImpression> _impressionsExpected = new List<KeyImpression>
        {
            new KeyImpression("nico_test", "FACUNDO_TEST", "on", 0, 1506703262916, "whitelisted", null, null, false),
            new KeyImpression("mauro_test", "FACUNDO_TEST", "off", 0, 1506703262916, "in segment all", null, null, false),
            new KeyImpression("1", "Test_Save_1", "on", 0, 1503956389520, "whitelisted", null, null, false),
            new KeyImpression("24", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, null, false),
            new KeyImpression("mauro", "MAURO_TEST", "on", 0, 1506703262966, "whitelisted", null, null, false),
            new KeyImpression("test", "MAURO_TEST", "off", 0, 1506703262966,"not in split", null, null, false),
            new KeyImpression("nico_test", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, null, false),
            new KeyImpression("nico_test", "MAURO_TEST", "off", 0, 1506703262966, "not in split", null, null, false),
            new KeyImpression("mauro", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, null, false)
        };

        public static KeyImpression GetImpressionExpected(string featureName, string key)
        {
            return _impressionsExpected.FirstOrDefault(i => i.feature.Equals(featureName) && i.keyName.Equals(key));
        }

        public static void AssertImpression(KeyImpression impression, KeyImpression expected)
        {
            Assert.AreEqual(expected.changeNumber, expected.changeNumber);
            Assert.AreEqual(expected.feature, impression.feature);
            Assert.AreEqual(expected.keyName, impression.keyName);
            Assert.AreEqual(expected.label, impression.label);
            Assert.AreEqual(expected.treatment, impression.treatment);
        }

        public static async Task AssertImpressionListenerAsync(string mode, int expected, IntegrationTestsImpressionListener impressionListener)
        {
            for (int i = 0; i < 3; i++)
            {
                if (impressionListener.Count() > 0)
                    break;

                await Task.Delay(1000);
            }

            Assert.AreEqual(expected, impressionListener.Count(), $"{mode}: Impression Listener not match");
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
