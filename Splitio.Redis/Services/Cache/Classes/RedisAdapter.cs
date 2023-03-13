using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapter : IRedisAdapter
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisAdapter));

        private readonly RedisConfig _config;
        private readonly IConnectionPoolManager _pool;

        public RedisAdapter(RedisConfig config,
            IConnectionPoolManager connectionPoolManager)
        {
            _config = config;
            _pool = connectionPoolManager;
        }

        #region Public Methods
        public bool IsConnected()
        {
            try { return GetServer()?.IsConnected ?? false; }
            catch { return false; }
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

        public bool Del(string key)
        {
            try
            {
                var db = GetDatabase();
                return db.KeyDelete(key);
            }
            catch (Exception e)
            {
                LogError("Del", key, e);
                return false;
            }
            finally { FinishProfiling("Del", key); }
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

        public long IcrBy(string key, long value)
        {
            try
            {
                var db = GetDatabase();
                return db.StringIncrement(key, value);
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

        public void Flush()
        {
            try
            {
                var server = GetServer();
                server.FlushDatabase();
            }
            catch (Exception e)
            {
                _log.Error("Exception calling Redis Adapter Flush", e);
            }
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

        // Only for tests.
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
        #endregion

        #region Private Methods
        private void FinishProfiling(string command, string key)
        {
#if NET_LATEST
            if (_config.LocalProfiler == null) return;

            var commands = _config.LocalProfiler.GetSession().FinishProfiling();

            var count = 1;
            foreach (var item in commands)
            {
                _log.Debug($"Count: {count++}\nKey: {key}\nMethod:{command}\nInfo: {item}");
            }
#endif
        }

        private void LogError(string command, string key, Exception ex)
        {
            _log.Error($"Exception calling Redis Adapter {command}.\nKey: {key}.\nMessage: {ex.Message}.\nStackTrace: {ex.StackTrace}.\n InnerExection: {ex.InnerException}.", ex);
        }

        private IServer GetServer()
        {
            var conn = _pool.GetConnection();

            return conn.GetServer(_config.HostAndPort);
        }

        private IDatabase GetDatabase()
        {
            var conn = _pool.GetConnection();

            return conn.GetDatabase(_config.RedisDatabase);
        }
        #endregion
    }
}
