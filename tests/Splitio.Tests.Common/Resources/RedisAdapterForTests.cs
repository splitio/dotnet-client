using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Splitio.Tests.Common.Resources
{
    public class RedisAdapterForTests : RedisAdapter
    {
        public RedisAdapterForTests(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager)
        {
        }

        public TimeSpan? KeyTimeToLive(RedisKey key)
        {
            try
            {
                var db = GetDatabase();
                return db.KeyTimeToLive(key);
            }
            catch
            {
                return null;
            }
        }

        public long Del(RedisKey[] keys)
        {
            try
            {
                var db = GetDatabase();
                return db.KeyDelete(keys);
            }
            catch (Exception e)
            {
                LogError("Del Keys", string.Empty, e);
                return 0;
            }
            finally { FinishProfiling("Del Keys", string.Empty); }
        }
        public async Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key)
        {
            try
            {
                var db = GetDatabase();
                return await db.KeyTimeToLiveAsync(key);
            }
            catch
            {
                return null;
            }
        }

        public async Task<long> DelAsync(RedisKey[] keys)
        {
            try
            {
                var db = GetDatabase();
                return await db.KeyDeleteAsync(keys);
            }
            catch (Exception e)
            {
                LogError(nameof(DelAsync), string.Empty, e);
                return 0;
            }
            finally { FinishProfiling(nameof(DelAsync), string.Empty); }
        }

        public bool Set(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return db.StringSet(key, value);
            }
            catch (Exception e)
            {
                LogError("Set", key, e);
                return false;
            }
            finally { FinishProfiling("Set", key); }
        }

        public bool SAdd(string key, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return db.SetAdd(key, value);
            }
            catch (Exception e)
            {
                LogError("SAdd", key, e);
                return false;
            }
            finally { FinishProfiling("SAdd", key); }
        }

        public long SAdd(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return db.SetAdd(key, values);
            }
            catch (Exception e)
            {
                LogError("SAdd", key, e);
                return 0;
            }
            finally { FinishProfiling("SAdd", key); }
        }

        public long SRem(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return db.SetRemove(key, values);
            }
            catch (Exception e)
            {
                LogError("SRem", key, e);
                return 0;
            }
            finally { FinishProfiling("SRem", key); }
        }

        public long IcrBy(string key, long delta)
        {
            try
            {
                var db = GetDatabase();
                return db.StringIncrement(key, delta);
            }
            catch (Exception e)
            {
                LogError("IcrBy", key, e);
                return 0;
            }
            finally { FinishProfiling("IcrBy", key); }
        }

        public async Task<bool> SetAsync(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringSetAsync(key, value);
            }
            catch (Exception e)
            {
                LogError(nameof(SetAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(SetAsync), key); }
        }

        public async Task<bool> SAddAsync(string key, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetAddAsync(key, value);
            }
            catch (Exception e)
            {
                LogError(nameof(SAddAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(SAddAsync), key); }
        }

        public async Task<long> SAddAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetAddAsync(key, values);
            }
            catch (Exception e)
            {
                LogError(nameof(SAddAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(SAddAsync), key); }
        }

        public async Task<long> SRemAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetRemoveAsync(key, values);
            }
            catch (Exception e)
            {
                LogError(nameof(SRemAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(SRemAsync), key); }
        }

        public async Task<long> IcrByAsync(string key, long delta)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringIncrementAsync(key, delta);
            }
            catch (Exception e)
            {
                LogError(nameof(IcrByAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(IcrByAsync), key); }
        }
    }
}
