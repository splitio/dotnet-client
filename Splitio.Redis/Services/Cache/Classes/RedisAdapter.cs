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

        public long HashIncrementBatch(string key, Dictionary<string, int> values)
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
        #endregion

        #region Public Methdos async
        public async Task<bool> SetAsync(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringSetAsync(key, value).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError(nameof(SetAsync), key, e);
                return false;
            }
            finally { FinishProfiling(nameof(SetAsync), key); }
        }

        public async Task<string> GetAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringGetAsync(key).ConfigureAwait(false);
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
                return await db.StringGetAsync(keys).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("MGet", string.Empty, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("MGet", string.Empty); }
        }

        public async Task<long> SAddAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetAddAsync(key, values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("SAdd", key, e);
                return 0;
            }
            finally { FinishProfiling("SAdd", key); }
        }

        public async Task<long> SRemAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetRemoveAsync(key, values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("SRem", key, e);
                return 0;
            }
            finally { FinishProfiling("SRem", key); }
        }

        public async Task<bool> SIsMemberAsync(string key, string value)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetContainsAsync(key, value).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("SIsMember", key, e);
                return false;
            }
            finally { FinishProfiling("SIsMember", key); }
        }

        public async Task<RedisValue[]> SMembersAsync(string key)
        {
            try
            {
                var db = GetDatabase();
                return await db.SetMembersAsync(key).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("SMembers", key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("SMembers", key); }
        }

        public async Task<long> IcrByAsync(string key, long value)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringIncrementAsync(key, value).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("IcrBy", key, e);
                return 0;
            }
            finally { FinishProfiling("IcrBy", key); }
        }

        public async Task<long> ListRightPushAsync(string key, RedisValue[] values)
        {
            try
            {
                var db = GetDatabase();
                return await db.ListRightPushAsync(key, values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("ListRightPush", key, e);
                return 0;
            }
            finally { FinishProfiling("ListRightPush", key); }
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                return await db.KeyExpireAsync(key, expiry).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("KeyExpire", key, e);
                return false;
            }
            finally { FinishProfiling("KeyExpire", key); }
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1)
        {
            try
            {
                var db = GetDatabase();
                return await db.ListRangeAsync(key, start, stop).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("ListRange", key, e);
                return new RedisValue[0];
            }
            finally { FinishProfiling("ListRange", key); }
        }

        public async Task<double> HashIncrementAsync(string key, string hashField, double value)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashIncrementAsync(key, hashField, value).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("HashIncrement", key, e);
                return 0;
            }
            finally { FinishProfiling("HashIncrement", key); }
        }

        public async Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashSetAsync(key, hashField, value).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("HashSet", key, e);
                return false;
            }
            finally { FinishProfiling("HashSet", key); }
        }

        public async Task<HashEntry[]> HashGetAllAsync(RedisKey key)
        {
            try
            {
                var db = GetDatabase();
                return await db.HashGetAllAsync(key).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogError("HashGetAll", key, e);
                return new HashEntry[0];
            }
            finally { FinishProfiling("HashGetAll", key); }
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

        #region Test Methods
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
        #endregion
    }
}
