using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapter
    {
        bool IsConnected();
        RedisKey[] Keys(string pattern);
        long HashIncrementBatch(string key, Dictionary<string, int> values);

        Task<bool> SetAsync(string key, string value);
        Task<string> GetAsync(string key);
        Task<RedisValue[]> MGetAsync(RedisKey[] keys);
        Task<long> SAddAsync(string key, RedisValue[] values);
        Task<long> SRemAsync(string key, RedisValue[] values);
        Task<bool> SIsMemberAsync(string key, string value);
        Task<RedisValue[]> SMembersAsync(string key);
        Task<long> IcrByAsync(string key, long delta);
        Task<long> ListRightPushAsync(string key, RedisValue[] values);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1);
        Task<double> HashIncrementAsync(string key, string field, double value);
        Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value);
        Task<HashEntry[]> HashGetAllAsync(RedisKey key);

        // Only for tests.
        TimeSpan? KeyTimeToLive(RedisKey key);
        void Flush();
        long Del(RedisKey[] keys);
    }
}
