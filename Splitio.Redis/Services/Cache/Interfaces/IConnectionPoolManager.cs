using StackExchange.Redis;
using System;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IConnectionPoolManager : IDisposable
    {
        IConnectionMultiplexer GetConnection();
    }
}
