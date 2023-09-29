using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using StackExchange.Redis;
using System;

namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class BaseAdapter
    {
        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisAdapter));

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

        protected static void LogError(string command, string key, Exception ex)
        {
            _log.Error($"Exception calling Redis Adapter {command}.\nKey: {key}.\nMessage: {ex.Message}.\nStackTrace: {ex.StackTrace}.\n InnerExection: {ex.InnerException}.", ex);
        }

        protected IServer GetServer()
        {
            var conn = _pool.GetConnection();

            return conn?.GetServer(_config.HostAndPort);
        }

        protected IDatabase GetDatabase()
        {
            var conn = _pool.GetConnection();

            return conn?.GetDatabase(_config.RedisDatabase);
        }
    }
}
