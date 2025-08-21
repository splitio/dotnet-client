using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Tests.Common.Resources
{
    public class RedisHelper
    {
        public static void AssertSentEvents(IRedisAdapterConsumer redisAdapter, string userPrefix, List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true)
        {
            Thread.Sleep(1000);

            var redisEvents = redisAdapter.ListRange($"{userPrefix}.SPLITIO.events");

            Assert.AreEqual(eventsExcpected.Count, redisEvents.Length);

            foreach (var item in redisEvents)
            {
                var actualEvent = JsonConvertWrapper.DeserializeObject<EventRedis>(item);

                AssertEvent(actualEvent, eventsExcpected);
            }
        }

        public static void AssertSentImpressions(IRedisAdapterConsumer redisAdapter, string userPrefix, int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            Thread.Sleep(1000);

            var redisImpressions = redisAdapter.ListRange($"{userPrefix}.SPLITIO.impressions");

            Assert.AreEqual(sentImpressionsCount, redisImpressions.Length);

            foreach (var item in redisImpressions)
            {
                var actualImp = JsonConvertWrapper.DeserializeObject<KeyImpressionRedis>(item);

                AssertImpression(actualImp, expectedImpressions.ToList());
            }
        }

        public static async Task LoadSplitsAsync(string rootFilePath, string userPrefix, RedisAdapterForTests redisAdapter)
        {
            await CleanupAsync(userPrefix, redisAdapter);

            var splitsJson = File.ReadAllText($"{rootFilePath}split_changes.json");

            var result = JsonConvertWrapper.DeserializeObject<TargetingRulesDto>(splitsJson);

            foreach (var split in result.FeatureFlags.Data)
            {
                await redisAdapter.SetAsync($"{userPrefix}.SPLITIO.split.{split.name}", JsonConvertWrapper.SerializeObject(split));

                if (split.Sets != null && split.Sets.Any())
                {
                    foreach (var fSet in split.Sets)
                    {
                        await redisAdapter.SAddAsync($"{userPrefix}.SPLITIO.flagSet.{fSet}", split.name);
                    }
                }
            }

            foreach (var rbs in result.RuleBasedSegments.Data)
            {
                await redisAdapter.SetAsync($"{userPrefix}.SPLITIO.rbsegment.{rbs.Name}", JsonConvertWrapper.SerializeObject(rbs));
            }
        }

        public static async Task CleanupAsync(string userPrefix, RedisAdapterForTests redisAdapter)
        {
            var keys = redisAdapter.Keys($"{userPrefix}*");
            await redisAdapter.DelAsync(keys);
        }

        public static void Cleanup(string userPrefix, RedisAdapterForTests redisAdapter)
        {
            var keys = redisAdapter.Keys($"{userPrefix}*");
            redisAdapter.Del(keys);
        }

        private static void AssertImpression(KeyImpressionRedis impressionActual, List<KeyImpression> sentImpressions)
        {
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.I));
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.N));
            Assert.IsFalse(string.IsNullOrEmpty(impressionActual.M.S));

            Assert.IsTrue(sentImpressions
                .Where(si => impressionActual.I.B == si.BucketingKey)
                .Where(si => impressionActual.I.C == si.ChangeNumber)
                .Where(si => impressionActual.I.K == si.KeyName)
                .Where(si => impressionActual.I.R == si.Label)
                .Where(si => impressionActual.I.T == si.Treatment)
                .Any());
        }

        private static void AssertEvent(EventRedis eventActual, List<EventBackend> eventsExcpected)
        {
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.I));
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.N));
            Assert.IsFalse(string.IsNullOrEmpty(eventActual.M.S));

            Assert.IsTrue(eventsExcpected
                .Where(ee => eventActual.E.EventTypeId == ee.EventTypeId)
                .Where(ee => eventActual.E.Key == ee.Key)
                .Where(ee => eventActual.E.Properties?.Count == ee.Properties?.Count)
                .Where(ee => eventActual.E.TrafficTypeName == ee.TrafficTypeName)
                .Where(ee => eventActual.E.Value == ee.Value)
                .Any());
        }
    }
}
