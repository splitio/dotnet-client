using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio.Tests.Common.Resources
{
    public class Helper
    {
        private readonly static List<KeyImpression> _impressionsExpected = new List<KeyImpression>
        {
            new KeyImpression("nico_test", "FACUNDO_TEST", "on", 0, 1506703262916, "whitelisted", null, false, null, false),
            new KeyImpression("mauro_test", "FACUNDO_TEST", "off", 0, 1506703262916, "in segment all", null, false, null, false),
            new KeyImpression("1", "Test_Save_1", "on", 0, 1503956389520, "whitelisted", null, false, null, false),
            new KeyImpression("24", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, false, null, false),
            new KeyImpression("mauro", "MAURO_TEST", "on", 0, 1506703262966, "whitelisted", null, false, null, false),
            new KeyImpression("test", "MAURO_TEST", "off", 0, 1506703262966,"not in split", null, false, null, false),
            new KeyImpression("nico_test", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, false, null, false),
            new KeyImpression("nico_test", "MAURO_TEST", "off", 0, 1506703262966, "not in split", null, false, null, false),
            new KeyImpression("mauro", "Test_Save_1", "off", 0, 1503956389520, "in segment all", null, false, null, false),
            new KeyImpression("nico_test", "feature_flag_for_test", "control", 0, 1709843458770, "unsupported matcher type", null, false, null, false),
            new KeyImpression("mauro_test", "semver_between", "on", 0, 1675259356568, "between semver", null, false, null, false),
            new KeyImpression("mauro_test2", "semver_between", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test_eq", "semver_equalto", "on", 0, 1675259356568, "equal to semver", null, false, null, false),
            new KeyImpression("test_eq2", "semver_equalto", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test_gtet", "semver_greater_or_equalto", "on", 0, 1675259356568, "greater than or equal to semver", null, false, null, false),
            new KeyImpression("test_gtet2", "semver_greater_or_equalto", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test_ltet", "semver_less_or_equalto", "on", 0, 1675259356568, "less than or equal to semver", null, false, null, false),
            new KeyImpression("test_ltet2", "semver_less_or_equalto", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test_list", "semver_inlist", "on", 0, 1675259356568, "in list semver", null, false, null, false),
            new KeyImpression("test_list2", "semver_inlist", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test1", "with_track_enabled", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test2", "with_track_disabled", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("test3", "without_track", "off", 0, 1675259356568, "default rule", null, false, null, false),
            new KeyImpression("mauro@split.io", "rbs_test_flag_negated", "v1", 0, 10, "not in rule based segment test_rule_based_segment", null, false, null, false),
            new KeyImpression("mauro@harness.io", "rbs_test_flag_negated", "v1", 0, 10, "not in rule based segment test_rule_based_segment", null, false, null, false),
            new KeyImpression("mauro.sanz@split.io", "rbs_test_flag_negated", "v2", 0, 10, "default rule", null, false, null, false),
            new KeyImpression("mauro@split.io", "rbs_test_flag", "v2", 0, 10, "default rule", null, false, null, false),
            new KeyImpression("mauro@harness.io", "rbs_test_flag", "v2", 0, 10, "default rule", null, false, null, false),
            new KeyImpression("mauro.sanz@split.io", "rbs_test_flag", "v1", 0, 10, "in rule based segment test_rule_based_segment", null, false, null, false),
            new KeyImpression("mauro@split.io", "always_on_if_prerequisite", "off", 0, 5, "prerequisites not met", null, false, null, false),
            new KeyImpression("bilal@split.io", "always_on_if_prerequisite", "on", 0, 5, "always_on_if_prerequisite label", null, false, null, false),
            new KeyImpression("other_key", "always_on_if_prerequisite", "off", 0, 5, "prerequisites not met", null, false, null, false),
        };

        public static KeyImpression GetImpressionExpected(string featureName, string key)
        {
            return _impressionsExpected.FirstOrDefault(i => i.Feature.Equals(featureName) && i.KeyName.Equals(key));
        }

        public static void AssertImpression(KeyImpression impression, KeyImpression expected)
        {
            Assert.AreEqual(expected.ChangeNumber, expected.ChangeNumber);
            Assert.AreEqual(expected.Feature, impression.Feature);
            Assert.AreEqual(expected.KeyName, impression.KeyName);
            Assert.AreEqual(expected.Label, impression.Label);
            Assert.AreEqual(expected.Treatment, impression.Treatment);
        }

        public static void AssertImpressionListener(string mode, int expected, IntegrationTestsImpressionListener impressionListener)
        {
            for (int i = 0; i < 5; i++)
            {
                if (impressionListener.Count() > 0)
                    break;

                Thread.Sleep(1000);
            }

            Assert.AreEqual(expected, impressionListener.Count(), $"{mode}: Impression Listener not match");
        }
    }

    public class InMemoryHelper
    {
        public static void AssertSentImpressions(int sentImpressionsCount, HttpClientMock httpClientMock = null, params KeyImpression[] expectedImpressions)
        {
            if (sentImpressionsCount <= 0) return;

            var sentImpressions = new List<KeyImpressionBackend>();

            for (int i = 0; i < 5; i++)
            {
                sentImpressions = GetImpressionsSentBackend(httpClientMock);

                if (sentImpressions.Count > 0) break;

                Thread.Sleep(1000);
            }

            Assert.AreEqual(sentImpressionsCount, sentImpressions.Sum(si => si.I.Count), "AssertSentImpressions Count");

            foreach (var expectedImp in expectedImpressions)
            {
                var impressions = new List<ImpressionData>();

                foreach (var ki in sentImpressions.Where(si => si.F.Equals(expectedImp.Feature)))
                {
                    impressions.AddRange(ki.I);
                }

                Assert.IsTrue(impressions
                    .Where(si => expectedImp.BucketingKey == si.B)
                    .Where(si => expectedImp.ChangeNumber == si.C)
                    .Where(si => expectedImp.KeyName == si.K)
                    .Where(si => expectedImp.Label == si.R)
                    .Where(si => expectedImp.Treatment == si.T)
                    .Any());
            }
        }

        public static List<KeyImpressionBackend> GetImpressionsSentBackend(HttpClientMock httpClientMock = null)
        {
            var impressions = new List<KeyImpressionBackend>();
            var logs = httpClientMock.GetImpressionLogs();

            foreach (var log in logs)
            {
                var imps = JsonConvert.DeserializeObject<List<KeyImpressionBackend>>(log.RequestMessage.Body);

                impressions.AddRange(imps);
            }

            return impressions;
        }

        public static void AssertSentEvents(List<EventBackend> eventsExpected, HttpClientMock httpClientMock = null, int? eventsCount = null, bool validateEvents = true)
        {
            var sentEvents = new List<EventBackend>();

            for (int i = 0; i < 5; i++)
            {
                sentEvents = GetEventsSentBackend(httpClientMock);

                if (sentEvents.Count > 0) break;

                Thread.Sleep(1000);
            }

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
