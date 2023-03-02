using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class ConnectionPoolManager : IConnectionPoolManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisAdapter));

        private readonly object _lock = new object();

        private readonly IConnectionMultiplexer[] _connections;
        private readonly Random _random;

        private int _lastIdx;

        public ConnectionPoolManager(RedisConfig config)
        {
            lock (_lock)
            {
                try
                {
                    _connections = new IConnectionMultiplexer[config.PoolSize];
                    _random = new Random();

                    var configuration = GetConfig(config);
                    for (int i = 0; i < config.PoolSize; i++)
                    {
                        var multiplexer = ConnectionMultiplexer.Connect(configuration);
#if NET_LATEST
                        if (config.LocalProfiler != null)
                            multiplexer.RegisterProfiler(config.LocalProfiler.GetSession);
#endif
                        _connections[i] = multiplexer;
                    }
                }
                catch (Exception e)
                {
                    _log.Error($"Exception caught Connecting to redis: '{config.HostAndPort}'", e);
                }
            }
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
                connection.Dispose();

            GC.SuppressFinalize(this);
        }

        public IConnectionMultiplexer GetConnection()
        {
            if (_connections == null || _connections.Length == 0) return null;

            IConnectionMultiplexer conn;

#if NET6_0 || NET7_0
            conn = _connections.MinBy(c => c.GetCounters().TotalOutstanding);
#else
            conn = _connections[GetRandomIdx()];
#endif

            _log.Debug($"Using connection {conn.GetHashCode()}.");

            return conn;
        }
        
        private int GetRandomIdx()
        {
            var idx = _random.Next(_connections.Length);

            if (idx != _lastIdx)
            {
                _lastIdx = idx;
                return idx;
            }

            return GetRandomIdx();
        }

        private ConfigurationOptions GetConfig(RedisConfig redisCfg)
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { redisCfg.HostAndPort },
                Password = redisCfg.RedisPassword,
                AllowAdmin = true,
                KeepAlive = 1
            };

            if (redisCfg.TlsConfig != null && redisCfg.TlsConfig.Ssl)
            {
                config.Ssl = redisCfg.TlsConfig.Ssl;
                config.SslHost = redisCfg.RedisHost;

                if (redisCfg.TlsConfig.CertificateValidationFunc != null)
                {
                    config.CertificateValidation += redisCfg.TlsConfig.CertificateValidationFunc.Invoke;
                }

                if (redisCfg.TlsConfig.CertificateSelectionFunc != null)
                {
                    config.CertificateSelection += redisCfg.TlsConfig.CertificateSelectionFunc.Invoke;
                }
            }

            if (redisCfg.RedisConnectTimeout > 0)
            {
                config.ConnectTimeout = redisCfg.RedisConnectTimeout;
            }

            if (redisCfg.RedisConnectRetry > 0)
            {
                config.ConnectRetry = redisCfg.RedisConnectRetry;
            }

            if (redisCfg.RedisSyncTimeout > 0)
            {
                config.SyncTimeout = redisCfg.RedisSyncTimeout;
            }

            return config;
        }
    }
}
