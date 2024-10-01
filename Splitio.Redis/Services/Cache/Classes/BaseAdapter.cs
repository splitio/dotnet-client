using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class BaseAdapter
    {
        protected readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(BaseAdapter));

        protected readonly RedisConfig _config;
        protected readonly IConnectionPoolManager _pool;

        public BaseAdapter(RedisConfig config,
            IConnectionPoolManager connectionPoolManager)
        {
            _config = config;
            _pool = connectionPoolManager;
        }

        protected void FinishProfiling(string command, string key)
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

        protected void LogError(string command, string key, Exception ex)
        {
            _log.Error($"Exception calling Redis Adapter {command}.\nKey: {key}.\nMessage: {ex.Message}.\nStackTrace: {ex.StackTrace}.\n InnerExection: {ex.InnerException}.", ex);
        }

        protected IServer GetServer()
        {
            var conn = _pool.GetConnection();

            return conn?.GetServer(_config.HostAndPort);
        }
        protected List<IServer> GetServers()
        {
            var conn = _pool.GetConnection();
            List<IServer> servers = new List<IServer>();
            foreach (System.Net.EndPoint endpoint in conn?.GetEndPoints())
            {
                servers.Add(conn.GetServer(endpoint));
            }
            return servers;
        }

        protected IDatabase GetDatabase()
        {
            var conn = _pool.GetConnection();

            return conn?.GetDatabase(_config.RedisDatabase);
        }
    }
}