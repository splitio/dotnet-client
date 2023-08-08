using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapter : IRedisAdapterAsync
    {
        // Producer
        bool Set(string key, string value);
        bool SAdd(string key, RedisValue value);
        long SAdd(string key, RedisValue[] values);
        long SRem(string key, RedisValue[] values);
        long ListRightPush(string key, RedisValue value);
        long ListRightPush(string key, RedisValue[] values);
        long IcrBy(string key, long delta);
        double HashIncrement(string key, string field, double value);
        bool HashSet(RedisKey key, RedisValue hashField, RedisValue value);
        long HashIncrementAsyncBatch(string key, Dictionary<string, int> values);
        bool KeyExpire(string key, TimeSpan expiry);

        // Consumer
        bool IsConnected();
        string Get(string key);
        RedisValue[] MGet(RedisKey[] keys);
        RedisKey[] Keys(string pattern);
        RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1);
        HashEntry[] HashGetAll(RedisKey key);
        RedisValue[] SMembers(string key);
        bool SIsMember(string key, string value);

        // Only for tests
        TimeSpan? KeyTimeToLive(RedisKey key);
        long Del(RedisKey[] keys);
    }
}
