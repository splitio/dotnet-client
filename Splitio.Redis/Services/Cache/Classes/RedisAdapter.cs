using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapter : RedisAdapterAsync, IRedisAdapter
    {
        public RedisAdapter(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager) { }

        #region Producer
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

        public long HashIncrementAsyncBatch(string key, Dictionary<string, int> values)
        {
            var tasks = new List<Task<long>>();
            long keysCount = 0;
            long hashLength = 0;
            var db = GetDatabase();

            try
            {
                foreach (var item in values)
                {
                    tasks.Add(db.HashIncrementAsync(key, item.Key, item.Value));
                }
            }
            catch (Exception e)
            {
                LogError("HashIncrementAsync", key, e);
            }
            finally
            {
                if (tasks.Any())
                {
                    Task.WaitAll(tasks.ToArray());

                    keysCount = tasks.Sum(t => t.Result);
                    hashLength = db.HashLengthAsync(key).Result;
                }

                FinishProfiling("HashIncrementAsync", key);
            }

            return keysCount + hashLength;
        }

        public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return db.HashSet(key, hashField, value);
            }
            catch (Exception e)
            {
                LogError("HashSet", key, e);
                return false;
            }
            finally { FinishProfiling("HashSet", key); }
        }
        #endregion

        #region Consumer
        public bool IsConnected()
        {
            try { return GetServer()?.IsConnected ?? false; }
            catch { return false; }
        }

        public string Get(string key)
        {
            try
            {
                var db = GetDatabase();
                return db.StringGet(key);
            }
            catch (Exception e)
            {
                LogError("Get", key, e);
                return string.Empty;
            }
            finally { FinishProfiling("Get", key); }
        }

        public RedisValue[] MGet(RedisKey[] keys)
        {
            try
            {
                var db = GetDatabase();
                return db.StringGet(keys);
            }
            catch (Exception e)
            {
                LogError("MGet", string.Empty, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("MGet", string.Empty); }
        }

        public RedisKey[] Keys(string pattern)
        {
            try
            {
                var server = GetServer();
                var keys = server.Keys(_config.RedisDatabase, pattern);
                return keys.ToArray();
            }
            catch (Exception e)
            {
                LogError("Keys", pattern, e);
                return new RedisKey[0];
            }
            finally { FinishProfiling("Keys", pattern); }
        }

        public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1)
        {
            try
            {
                var db = GetDatabase();
                return db.ListRange(key, start, stop);
            }
            catch (Exception e)
            {
                LogError("ListRange", key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("ListRange", key); }
        }

        public HashEntry[] HashGetAll(RedisKey key)
        {
            try
            {
                var db = GetDatabase();
                return db.HashGetAll(key);
            }
            catch (Exception e)
            {
                LogError("HashGetAll", key, e);
                return new HashEntry[0];
            }
            finally { FinishProfiling("HashGetAll", key); }
        }

        public RedisValue[] SMembers(string key)
        {
            try
            {
                var db = GetDatabase();
                return db.SetMembers(key);
            }
            catch (Exception e)
            {
                LogError("SMembers", key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("SMembers", key); }
        }

        public bool SIsMember(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return db.SetContains(key, value);
            }
            catch (Exception e)
            {
                LogError("SIsMember", key, e);
                return false;
            }
            finally { FinishProfiling("SIsMember", key); }
        }
        #endregion

        #region Only for tests
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
        #endregion
    }
}
