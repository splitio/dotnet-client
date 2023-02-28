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

        private readonly object _lock = new object();

        private readonly RedisConfig _config;
        private readonly IConnectionMultiplexer _redis;
        private readonly IServer _server;
#if NET_LATEST
        private readonly AsyncLocalProfiler _profiler;
#endif

        public RedisAdapter(RedisConfig config)
        {
            _config = config;

            try
            {
                _redis = ConnectionMultiplexer.Connect(GetConfig());
                _server = _redis.GetServer(_config.HostAndPort);

#if NET_LATEST
                if (_config.ProfilingEnabled)
                {
                    _profiler = new AsyncLocalProfiler();
                    _redis.RegisterProfiler(_profiler.GetSession);
                }
#endif
            }
            catch (Exception e)
            {
                _log.Error($"Exception caught building Redis Adapter '{_config.HostAndPort}': {e}");
            }
        }

        #region Public Methods
        public bool IsConnected()
        {
            return _server?.IsConnected ?? false;
        }

        public bool Set(string key, string value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.StringSet(key, value);
                }
                catch (Exception e)
                {
                    LogError("Set", key, e);
                    return false;
                }
                finally { FinishProfiling("Set", key); }
            }
        }

        public string Get(string key)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.StringGet(key);
                }
                catch (Exception e)
                {
                    LogError("Get", key, e);
                    return string.Empty;
                }
                finally { FinishProfiling("Get", key); }
            }
        }

        public RedisValue[] MGet(RedisKey[] keys)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.StringGet(keys);
                }
                catch (Exception e)
                {
                    LogError("MGet", string.Empty, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling("MGet", string.Empty); }
            }
        }

        public RedisKey[] Keys(string pattern)
        {
            lock (_lock)
            {
                try
                {
                    var keys = _server.Keys(_config.RedisDatabase, pattern);
                    return keys.ToArray();
                }
                catch (Exception e)
                {
                    LogError("Keys", pattern, e);
                    return new RedisKey[0];
                }
                finally { FinishProfiling("Keys", pattern); }
            }
        }

        public bool Del(string key)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.KeyDelete(key);
                }
                catch (Exception e)
                {
                    LogError("Del", key, e);
                    return false;
                }
                finally { FinishProfiling("Del", key); }
            }
        }

        public long Del(RedisKey[] keys)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.KeyDelete(keys);
                }
                catch (Exception e)
                {
                    LogError("Del Keys", string.Empty, e);
                    return 0;
                }
                finally { FinishProfiling("Del Keys", string.Empty); }
            }
        }

        public bool SAdd(string key, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.SetAdd(key, value);
                }
                catch (Exception e)
                {
                    LogError("SAdd", key, e);
                    return false;
                }
                finally { FinishProfiling("SAdd", key); }
            }
        }

        public long SAdd(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.SetAdd(key, values);
                }
                catch (Exception e)
                {
                    LogError("SAdd", key, e);
                    return 0;
                }
                finally { FinishProfiling("SAdd", key); }
            }
        }

        public long SRem(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.SetRemove(key, values);
                }
                catch (Exception e)
                {
                    LogError("SRem", key, e);
                    return 0;
                }
                finally { FinishProfiling("SRem", key); }
            }
        }

        public bool SIsMember(string key, string value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.SetContains(key, value);
                }
                catch (Exception e)
                {
                    LogError("SIsMember", key, e);
                    return false;
                }
                finally { FinishProfiling("SIsMember", key); }
            }
        }

        public RedisValue[] SMembers(string key)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.SetMembers(key);
                }
                catch (Exception e)
                {
                    LogError("SMembers", key, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling("SMembers", key); }
            }
        }

        public long IcrBy(string key, long value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.StringIncrement(key, value);
                }
                catch (Exception e)
                {
                    LogError("IcrBy", key, e);
                    return 0;
                }
                finally { FinishProfiling("IcrBy", key); }
            }
        }

        public long ListRightPush(string key, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.ListRightPush(key, value);
                }
                catch (Exception e)
                {
                    LogError("ListRightPush", key, e);
                    return 0;
                }
                finally { FinishProfiling("ListRightPush", key); }
            }
        }

        public void Flush()
        {
            try
            {
                _server.FlushDatabase();
            }
            catch (Exception e)
            {
                _log.Error("Exception calling Redis Adapter Flush", e);
            }
        }

        public bool KeyExpire(string key, TimeSpan expiry)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.KeyExpire(key, expiry);
                }
                catch (Exception e)
                {
                    LogError("KeyExpire", key, e);
                    return false;
                }
                finally { FinishProfiling("KeyExpire", key); }
            }
        }

        public long ListRightPush(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.ListRightPush(key, values);
                }
                catch (Exception e)
                {
                    LogError("ListRightPush", key, e);
                    return 0;
                }
                finally { FinishProfiling("ListRightPush", key); }
            }
        }

        public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.ListRange(key, start, stop);
                }
                catch (Exception e)
                {
                    LogError("ListRange", key, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling("ListRange", key); }
            }
        }

        public double HashIncrement(string key, string hashField, double value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.HashIncrement(key, hashField, value);
                }
                catch (Exception e)
                {
                    LogError("HashIncrement", key, e);
                    return 0;
                }
                finally { FinishProfiling("HashIncrement", key); }
            }
        }

        public long HashIncrementAsyncBatch(string key, Dictionary<string, int> values)
        {
            lock (_lock)
            {
                var tasks = new List<Task<long>>();
                long keysCount = 0;
                long hashLength = 0;
                var db = _redis.GetDatabase(_config.RedisDatabase);

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
        }

        public HashEntry[] HashGetAll(RedisKey key)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.HashGetAll(key);
                }
                catch (Exception e)
                {
                    LogError("HashGetAll", key, e);
                    return new HashEntry[0];
                }
                finally { FinishProfiling("HashGetAll", key); }
            }
        }

        public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    var db = _redis.GetDatabase(_config.RedisDatabase);
                    return db.HashSet(key, hashField, value);
                }
                catch (Exception e)
                {
                    LogError("HashSet", key, e);
                    return false;
                }
                finally { FinishProfiling("HashSet", key); }
            }
        }
        #endregion

        #region Private Methods
        private void FinishProfiling(string command, string key)
        {
            Profiling(command, key, true);
        }

        private void Profiling(string command, string key, bool forced = false)
        {
#if NET_LATEST
            if (!_config.ProfilingEnabled) return;

            var commands = _profiler.GetSession().FinishProfiling();

            if (forced && commands.Count() <= 1) return;

            var count = 1;
            foreach (var item in commands)
            {
                _log.Debug($"Count: {count++}\nKey: {key}\nMethod:{command}\nInfo: {item}");
            }
#endif
        }

        private void LogError(string command, string key, Exception ex)
        {
            Profiling(command, key);
            _log.Error($"Exception calling Redis Adapter {command}.\nKey: {key}.\nMessage: {ex.Message}.\nStackTrace: {ex.StackTrace}.\n InnerExection: {ex.InnerException}.", ex);
        }

        // public only for testing.
        public ConfigurationOptions GetConfig()
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { _config.HostAndPort },
                Password = _config.RedisPassword,
                AllowAdmin = true,
                KeepAlive = 1
            };

            if (_config.TlsConfig != null && _config.TlsConfig.Ssl)
            {
                config.Ssl = _config.TlsConfig.Ssl;
                config.SslHost = _config.RedisHost;

                if (_config.TlsConfig.CertificateValidationFunc != null)
                {
                    config.CertificateValidation += _config.TlsConfig.CertificateValidationFunc.Invoke;
                }

                if (_config.TlsConfig.CertificateSelectionFunc != null)
                {
                    config.CertificateSelection += _config.TlsConfig.CertificateSelectionFunc.Invoke;
                }
            }      

            if (_config.RedisConnectTimeout > 0)
            {
                config.ConnectTimeout = _config.RedisConnectTimeout;
            }

            if (_config.RedisConnectRetry > 0)
            {
                config.ConnectRetry = _config.RedisConnectRetry;
            }

            if (_config.RedisSyncTimeout > 0)
            {
                config.SyncTimeout = _config.RedisSyncTimeout;
            }

            return config;
        }
        #endregion
    }
}
