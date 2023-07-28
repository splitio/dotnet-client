using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapterAsync : BaseAdapter, IRedisAdapterAsync
    {
        public RedisAdapterAsync(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager) { }

        #region Producer
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

        public async Task<long> ListRightPushAsync(string key, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return await db.ListRightPushAsync(key, value);
            }
            catch (Exception e)
            {
                LogError(nameof(ListRightPushAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(ListRightPushAsync), key); }
        }

        public async Task<long> ListRightPushAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.ListRightPushAsync(key, values);
            }
            catch (Exception e)
            {
                LogError(nameof(ListRightPushAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(ListRightPushAsync), key); }
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

        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                return await db.KeyExpireAsync(key, expiry);
            }
            catch (Exception e)
            {
                LogError(nameof(KeyExpireAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(KeyExpireAsync), key); }
        }

        public async Task<double> HashIncrementAsync(string key, string field, double value)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashIncrementAsync(key, field, value);
            }
            catch (Exception e)
            {
                LogError(nameof(HashIncrementAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(HashIncrementAsync), key); }
        }

        public async Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashSetAsync(key, hashField, value);
            }
            catch (Exception e)
            {
                LogError(nameof(HashSetAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(HashSetAsync), key); }
        }

        public async Task<long> HashIncrementBatchAsync(string key, Dictionary<string, int> values)
        {
            try
            {
                var db = GetDatabase();

                long keysCount = 0;
                foreach (var item in values)
                {
                    keysCount += await db.HashIncrementAsync(key, item.Key, item.Value);
                }

                var hashLength = await db.HashLengthAsync(key);

                return keysCount + hashLength;
            }
            catch (Exception e)
            {
                LogError(nameof(HashIncrementBatchAsync), key, e);
                return 0;
            }
            finally { FinishProfiling(nameof(HashIncrementBatchAsync), key); }
        }
        #endregion

        #region Consumer
        public async Task<string> GetAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringGetAsync(key);
            }
            catch (Exception e)
            {
                LogError(nameof(GetAsync), key, e);
                return string.Empty;
            }
            finally { FinishProfiling(nameof(GetAsync), key); }
        }

        public async Task<RedisValue[]> MGetAsync(RedisKey[] keys)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringGetAsync(keys);
            }
            catch (Exception e)
            {
                LogError(nameof(MGetAsync), string.Empty, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling(nameof(MGetAsync), string.Empty); }
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1)
        {
            try
            {
                var db = GetDatabase();
                return await db.ListRangeAsync(key, start, stop);
            }
            catch (Exception e)
            {
                LogError(nameof(ListRangeAsync), key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling(nameof(ListRangeAsync), key); }
        }

        public async Task<HashEntry[]> HashGetAllAsync(RedisKey key)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashGetAllAsync(key);
            }
            catch (Exception e)
            {
                LogError(nameof(HashGetAllAsync), key, e);
                return new HashEntry[0];
            }
            finally { FinishProfiling(nameof(HashGetAllAsync), key); }
        }

        public async Task<RedisValue[]> SMembersAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetMembersAsync(key);
            }
            catch (Exception e)
            {
                LogError(nameof(SMembersAsync), key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling(nameof(SMembersAsync), key); }
        }

        public async Task<bool> SIsMemberAsync(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetContainsAsync(key, value);
            }
            catch (Exception e)
            {
                LogError(nameof(SIsMemberAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(SIsMemberAsync), key); }
        }
        #endregion

        #region Only for Tests
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
        #endregion
    }
}
