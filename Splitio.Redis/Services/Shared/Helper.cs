using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;
using System.Linq;

namespace Splitio.Redis.Services.Shared
{
    public static class Helper
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Helper));

        public static ConfigurationOptions ParseFromRedisConfig(RedisConfig redisCfg, ref bool isClusterMode)
        {
            var config = new ConfigurationOptions
            {
                Password = redisCfg.RedisPassword,
                AllowAdmin = true,
                KeepAlive = 1
            };

            if (redisCfg.ClusterNodes != null)
            {
                foreach (var host in redisCfg.ClusterNodes.EndPoints)
                {
                    config.EndPoints.Add(host);
                }
                isClusterMode = true;
            }
            else
            {
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

        public static ConfigurationOptions ParseFromConnectionString(RedisConfig redisCfg, ref bool isClusterMode)
        {
            try
            {
                var options = ConfigurationOptions.Parse(redisCfg.ConnectionString);
                options.AllowAdmin = true;
                options.KeepAlive = 1;

                if (!options.EndPoints.Any())
                {
                    _log.Warn("No endpoints detected in Redis Connection String, Redis connection might fail.");
                }

                if (!string.IsNullOrEmpty(redisCfg.RedisHost) || redisCfg.ClusterNodes != null)
                {
                    _log.Warn("Redis ConnectionString is set, will ignore other connection parameters.");
                }

                if (options.EndPoints.Count > 1)
                {
                    _log.Debug("Detected multiple redis hosts, will set the KeyHashTag to {SPLITIO}.");
                    isClusterMode = true;
                }

                return options;
            }
            catch (Exception e)
            {
                _log.Error($"Exception caught: Invalid Redis Connection String: {redisCfg.ConnectionString}.", e);
                return new ConfigurationOptions();
            }
        }
    }
}