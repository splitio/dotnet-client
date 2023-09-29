using StackExchange.Redis;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisAdapterConsumer
    {
        bool IsConnected();
        string Get(string key);
        RedisValue[] MGet(RedisKey[] keys);
        RedisKey[] Keys(string pattern);
        RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1);
        HashEntry[] HashGetAll(RedisKey key);
        RedisValue[] SMembers(string key);
        bool SIsMember(string key, string value);

        Task<string> GetAsync(string key);
        Task<RedisValue[]> MGetAsync(RedisKey[] keys);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1);
        Task<HashEntry[]> HashGetAllAsync(RedisKey key);
        Task<RedisValue[]> SMembersAsync(string key);
        Task<bool> SIsMemberAsync(string key, string value);
    }
}
