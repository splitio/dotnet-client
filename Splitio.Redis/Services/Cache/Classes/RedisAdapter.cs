using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
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

        private readonly string _host;
        private readonly string _port;
        private readonly string _password = "";
        private readonly int _databaseNumber = 0;
        private readonly int _connectTimeout = 0;
        private readonly int _connectRetry = 0;
        private readonly int _syncTimeout = 0;
        private readonly TlsConfig _tlsConfig;

        private IConnectionMultiplexer _redis;
        private IDatabase _database;
        private IServer _server;

#if NETSTANDARD2_0 || NET6_0 || NET5_0
        private readonly AsyncLocalProfiler _profiler;
#endif

        public RedisAdapter(string host,
            string port,
            string password = "",
            int databaseNumber = 0,
            int connectTimeout = 0,
            int connectRetry = 0,
            int syncTimeout = 0,
            TlsConfig tlsConfig = null)
        {
            _host = host;
            _port = port;
            _password = password;
            _databaseNumber = databaseNumber;
            _connectTimeout = connectTimeout;
            _connectRetry = connectRetry;
            _syncTimeout = syncTimeout;
            _tlsConfig = tlsConfig;

#if NETSTANDARD2_0 || NET6_0 || NET5_0
            _profiler = new AsyncLocalProfiler();
#endif
        }

        #region Public Methods
        public void Connect()
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(GetConfig());
                _database = _redis.GetDatabase(_databaseNumber);
                _server = _redis.GetServer($"{_host}:{_port}");

#if NETSTANDARD2_0 || NET6_0 || NET5_0
                _redis.RegisterProfiler(_profiler.GetSession);
#endif
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Exception caught building Redis Adapter '{0}:{1}': ", _host, _port), e);
            }
        }

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
                    return _database.StringSet(key, value);
                }
                catch (Exception e)
                {
                    LogError("Set", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }

        public string Get(string key)
        {
            lock (_lock)
            {
                try
                {
                    return _database.StringGet(key);
                }
                catch (Exception e)
                {
                    LogError("Get", key, e);
                    return string.Empty;
                }
                finally { FinishProfiling(); }
            }
        }

        public RedisValue[] MGet(RedisKey[] keys)
        {
            lock (_lock)
            {
                try
                {
                    return _database.StringGet(keys);
                }
                catch (Exception e)
                {
                    LogError("MGet", string.Empty, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling(); }
            }
        }

        public RedisKey[] Keys(string pattern)
        {
            lock (_lock)
            {
                try
                {
                    var keys = _server.Keys(_databaseNumber, pattern);
                    return keys.ToArray();
                }
                catch (Exception e)
                {
                    LogError("Keys", pattern, e);
                    return new RedisKey[0];
                }
                finally { FinishProfiling(); }
            }
        }

        public bool Del(string key)
        {
            lock (_lock)
            {
                try
                {
                    return _database.KeyDelete(key);
                }
                catch (Exception e)
                {
                    LogError("Del", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }

        public long Del(RedisKey[] keys)
        {
            lock (_lock)
            {
                try
                {
                    return _database.KeyDelete(keys);
                }
                catch (Exception e)
                {
                    LogError("Del Keys", string.Empty, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public bool SAdd(string key, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.SetAdd(key, value);
                }
                catch (Exception e)
                {
                    LogError("SAdd", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }

        public long SAdd(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    return _database.SetAdd(key, values);
                }
                catch (Exception e)
                {
                    LogError("SAdd", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public long SRem(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    return _database.SetRemove(key, values);
                }
                catch (Exception e)
                {
                    LogError("SRem", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public bool SIsMember(string key, string value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.SetContains(key, value);
                }
                catch (Exception e)
                {
                    LogError("SIsMember", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }

        public RedisValue[] SMembers(string key)
        {
            lock (_lock)
            {
                try
                {
                    return _database.SetMembers(key);
                }
                catch (Exception e)
                {
                    LogError("SMembers", key, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling(); }
            }
        }

        public long IcrBy(string key, long value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.StringIncrement(key, value);
                }
                catch (Exception e)
                {
                    LogError("IcrBy", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public long ListRightPush(string key, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.ListRightPush(key, value);
                }
                catch (Exception e)
                {
                    LogError("ListRightPush", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
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
                    return _database.KeyExpire(key, expiry);
                }
                catch (Exception e)
                {
                    LogError("KeyExpire", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }

        public long ListRightPush(string key, RedisValue[] values)
        {
            lock (_lock)
            {
                try
                {
                    return _database.ListRightPush(key, values);
                }
                catch (Exception e)
                {
                    LogError("ListRightPush", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1)
        {
            lock (_lock)
            {
                try
                {
                    return _database.ListRange(key, start, stop);
                }
                catch (Exception e)
                {
                    LogError("ListRange", key, e);
                    return new RedisValue[0];
                }
                finally { FinishProfiling(); }
            }
        }

        public double HashIncrement(string key, string hashField, double value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.HashIncrement(key, hashField, value);
                }
                catch (Exception e)
                {
                    LogError("HashIncrement", key, e);
                    return 0;
                }
                finally { FinishProfiling(); }
            }
        }

        public long HashIncrementAsyncBatch(string key, Dictionary<string, int> values)
        {
            lock (_lock)
            {
                var tasks = new List<Task<long>>();
                long keysCount = 0;
                long hashLength = 0;

                try
                {
                    foreach (var item in values)
                    {
                        tasks.Add(_database.HashIncrementAsync(key, item.Key, item.Value));
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
                        hashLength = _database.HashLengthAsync(key).Result;
                    }

                    FinishProfiling();
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
                    return _database.HashGetAll(key);
                }
                catch (Exception e)
                {
                    LogError("HashGetAll", key, e);
                    return new HashEntry[0];
                }
                finally { FinishProfiling(); }
            }
        }

        public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value)
        {
            lock (_lock)
            {
                try
                {
                    return _database.HashSet(key, hashField, value);
                }
                catch (Exception e)
                {
                    LogError("HashSet", key, e);
                    return false;
                }
                finally { FinishProfiling(); }
            }
        }
        #endregion

        #region Private Methods
        private void FinishProfiling()
        {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
            _log.Info("FinishProfiling");
            _profiler.GetSession().FinishProfiling();
#endif
        }

        private void Profiling()
        {
#if NETSTANDARD2_0 || NET6_0 || NET5_0
            _log.Info("Redis Profiling");
            var commands = _profiler.GetSession().FinishProfiling();

            foreach (var item in commands)
            {
                _log.Warn(item.ToString());
            }
#endif
        }

        private void LogError(string command, string key, Exception ex)
        {
            Profiling(); 
            _log.Error($"Exception calling Redis Adapter {command}.\nKey: {key}.\nMessage: {ex.Message}.\nStackTrace: {ex.StackTrace}.\n InnerExection: {ex.InnerException}.", ex);
        }

        // public only for testing.
        public ConfigurationOptions GetConfig()
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { $"{_host}:{_port}" },
                Password = _password,
                AllowAdmin = true,
                KeepAlive = 1,
            };

            if (_tlsConfig != null && _tlsConfig.Ssl)
            {
                config.Ssl = _tlsConfig.Ssl;
                config.SslHost = _host;

                if (_tlsConfig.CertificateValidationFunc != null)
                {
                    config.CertificateValidation += _tlsConfig.CertificateValidationFunc.Invoke;
                }

                if (_tlsConfig.CertificateSelectionFunc != null)
                {
                    config.CertificateSelection += _tlsConfig.CertificateSelectionFunc.Invoke;
                }
            }      

            if (_connectTimeout > 0)
            {
                config.ConnectTimeout = _connectTimeout;
            }

            if (_connectRetry > 0)
            {
                config.ConnectRetry = _connectRetry;
            }

            if (_syncTimeout > 0)
            {
                config.SyncTimeout = _syncTimeout;
            }

            return config;
        }
        #endregion
    }
}
