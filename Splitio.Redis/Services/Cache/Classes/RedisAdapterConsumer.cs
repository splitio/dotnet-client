using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapterConsumer : RedisAdapterProducer, IRedisAdapterConsumer
    {
        public RedisAdapterConsumer(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager)
        {
        }

        #region Sync Methods
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

        #region Async Methods
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

        public async Task<RedisKey[]> KeysAsync(string pattern)
        {
            try
            {
                var server = GetServer();
#if NET45
                return await Task.FromResult(Keys(pattern));
#else
                var keys = server.KeysAsync(_config.RedisDatabase, pattern);
                return await keys.ToArrayAsync();
#endif
            }
            catch (Exception e)
            {
                LogError("Keys", pattern, e);
                return new RedisKey[0];
            }
            finally { FinishProfiling("Keys", pattern); }
        }
#endregion
    }
}
