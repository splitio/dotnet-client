using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapterProducer
    {
        long ListRightPush(string key, RedisValue value);
        long ListRightPush(string key, RedisValue[] values);
        double HashIncrement(string key, string field, double value);
        bool HashSet(RedisKey key, RedisValue hashField, RedisValue value);
        long HashIncrementAsyncBatch(string key, Dictionary<string, int> values);
        bool KeyExpire(string key, TimeSpan expiry);

        Task<long> ListRightPushAsync(string key, RedisValue value);
        Task<long> ListRightPushAsync(string key, RedisValue[] values);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        Task<double> HashIncrementAsync(string key, string field, double value);
        Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value);
        Task<long> HashIncrementBatchAsync(string key, Dictionary<string, int> values);
    }
}
