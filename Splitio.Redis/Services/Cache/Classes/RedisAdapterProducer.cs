using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapterProducer : BaseAdapter, IRedisAdapterProducer
    {
        public RedisAdapterProducer(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager)
        {
        }

        #region Sync Methods
        public long ListRightPush(string key, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return db.ListRightPush(key, value);
            }
            catch (Exception e)
            {
                LogError("ListRightPush", key, e);
                return 0;
            }
            finally { FinishProfiling("ListRightPush", key); }
        }

        public bool KeyExpire(string key, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                return db.KeyExpire(key, expiry);
            }
            catch (Exception e)
            {
                LogError("KeyExpire", key, e);
                return false;
            }
            finally { FinishProfiling("KeyExpire", key); }
        }

        public long ListRightPush(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return db.ListRightPush(key, values);
            }
            catch (Exception e)
            {
                LogError("ListRightPush", key, e);
                return 0;
            }
            finally { FinishProfiling("ListRightPush", key); }
        }

        public double HashIncrement(string key, string hashField, double value)
        {
            try
            {
                var db = GetDatabase();
                return db.HashIncrement(key, hashField, value);
            }
            catch (Exception e)
            {
                LogError("HashIncrement", key, e);
                return 0;
            }
            finally { FinishProfiling("HashIncrement", key); }
        }
        #endregion

        #region Async Methods
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
                var tasks = new List<Task<long>>();
                var db = GetDatabase();

                foreach (var item in values)
                {
                    tasks.Add(db.HashIncrementAsync(key, item.Key, item.Value));
                }

                if (!tasks.Any()) return 0;
                
                var result = await Task.WhenAll(tasks.ToArray());
                var keysCount = result.Sum();
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
    }
}
