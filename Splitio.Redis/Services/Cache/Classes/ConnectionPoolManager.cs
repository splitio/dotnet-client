using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class ConnectionPoolManager : IConnectionPoolManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(ConnectionPoolManager));
        private readonly object _lock = new object();

        private readonly IConnectionMultiplexer[] _connections;
        private readonly Random _random;

        private bool _disposed;

        public ConnectionPoolManager(RedisConfig config)
        {
            lock (_lock)
            {
                _random = new Random();

                var conns = new List<IConnectionMultiplexer>();
                var configOptions = GetConfig(config);

                for (int i = 0; i < config.PoolSize; i++)
                {
                    try
                    {
                        var multiplexer = ConnectionMultiplexer.Connect(configOptions);
#if NET_LATEST
                        if (config.LocalProfiler != null)
                            multiplexer.RegisterProfiler(config.LocalProfiler.GetSession);
#endif
                        conns.Add(multiplexer);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Exception caught Connecting to redis: {config.HostAndPort}. Index: {i}", e);
                    }
                }

                _connections = conns.ToArray();
                _log.Info($"Total Pool Connections: {_connections.Length}");
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public IConnectionMultiplexer GetConnection()
        {
            lock (_lock)
            {
                if (_connections == null || _connections.Length == 0 || _connections[0] == null) return null;

                IConnectionMultiplexer conn;

#if NET6_0 || NET7_0
                conn = _connections.MinBy(c => c.GetCounters().TotalOutstanding);
                _log.Debug($"Using connection {conn.GetHashCode()}. Counters: {conn.GetCounters().TotalOutstanding}");
#else
                conn = _connections[_random.Next(_connections.Length)];
                _log.Debug($"Using connection {conn.GetHashCode()}.");
#endif

                return conn;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var conn in _connections)
                conn.Dispose();

            _disposed = true;
        }

        private static ConfigurationOptions GetConfig(RedisConfig redisCfg)
        {
            var config = new ConfigurationOptions
            {
                Password = redisCfg.RedisPassword,
                AllowAdmin = true,
                KeepAlive = 1
            };
            if (redisCfg.ClusterMode)
            {
                foreach (string host in redisCfg.ClusterNodes)
                {
                    config.EndPoints.Add(host);
                }
            } else {
                config.EndPoints.Add(redisCfg.HostAndPort);
            }

            if (redisCfg.TlsConfig != null && redisCfg.TlsConfig.Ssl)
            {
                config.Ssl = redisCfg.TlsConfig.Ssl;

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