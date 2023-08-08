using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapterAsync
    {
        // Producer
        Task<bool> SetAsync(string key, string value);
        Task<bool> SAddAsync(string key, RedisValue value);
        Task<long> SAddAsync(string key, RedisValue[] values);
        Task<long> SRemAsync(string key, RedisValue[] values);
        Task<long> ListRightPushAsync(string key, RedisValue value);
        Task<long> ListRightPushAsync(string key, RedisValue[] values);
        Task<long> IcrByAsync(string key, long delta);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        Task<double> HashIncrementAsync(string key, string field, double value);
        Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value);
        Task<long> HashIncrementBatchAsync(string key, Dictionary<string, int> values);

        // Consumer
        Task<string> GetAsync(string key);
        Task<RedisValue[]> MGetAsync(RedisKey[] keys);
        // TODO: KeysAsync is not avaiable fot netframework 4.5 and i need to install linq.async
        //Task<RedisKey[]> KeysAsync(string pattern);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1);
        Task<HashEntry[]> HashGetAllAsync(RedisKey key);
        Task<RedisValue[]> SMembersAsync(string key);
        Task<bool> SIsMemberAsync(string key, string value);

        // Only for tests
        //Task<bool> IsConnectedAsync(); Only have sync method.
        Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key);
        Task<long> DelAsync(RedisKey[] keys);
    }
}
