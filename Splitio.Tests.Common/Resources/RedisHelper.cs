using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Tests.Common.Resources
{
    public class RedisHelper
    {
        public static async Task AssertSentEventsAsync(IRedisAdapter redisAdapter, string userPrefix, List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true)
        {
            await Task.Delay(1500);

            var redisEvents = redisAdapter.ListRange($"{userPrefix}.SPLITIO.events");

            Assert.AreEqual(eventsExcpected.Count, redisEvents.Length);

            foreach (var item in redisEvents)
            {
                var actualEvent = JsonConvert.DeserializeObject<EventRedis>(item);

                AssertEvent(actualEvent, eventsExcpected);
            }
        }

        public static async Task AssertSentImpressionsAsync(IRedisAdapter redisAdapter, string userPrefix, int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            await Task.Delay(1500);

            var redisImpressions = redisAdapter.ListRange($"{userPrefix}.SPLITIO.impressions");

            Assert.AreEqual(sentImpressionsCount, redisImpressions.Length);

            foreach (var item in redisImpressions)
            {
                var actualImp = JsonConvert.DeserializeObject<KeyImpressionRedis>(item);

                AssertImpression(actualImp, expectedImpressions.ToList());
            }
        }

        public static async Task LoadSplitsAsync(string rootFilePath, string userPrefix, RedisAdapterForTests redisAdapter)
        {
            await CleanupAsync(userPrefix, redisAdapter);

            var splitsJson = File.ReadAllText($"{rootFilePath}split_changes.json");

            var splitResult = JsonConvert.DeserializeObject<SplitChangesResult>(splitsJson);

            foreach (var split in splitResult.splits)
            {
                await redisAdapter.SetAsync($"{userPrefix}.SPLITIO.split.{split.name}", JsonConvert.SerializeObject(split));

                if (split.Sets != null && split.Sets.Any())
                {
                    foreach (var fSet in split.Sets)
                    {
                        await redisAdapter.SAddAsync($"{userPrefix}.SPLITIO.flagSet.{fSet}", split.name);
                    }
                }
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
                .Where(si => impressionActual.I.B == si.bucketingKey)
                .Where(si => impressionActual.I.C == si.changeNumber)
                .Where(si => impressionActual.I.K == si.keyName)
                .Where(si => impressionActual.I.R == si.label)
                .Where(si => impressionActual.I.T == si.treatment)
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
