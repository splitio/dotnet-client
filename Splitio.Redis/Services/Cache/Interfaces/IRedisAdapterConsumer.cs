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
        // TODO: KeysAsync is not avaiable fot netframework 4.5 and i need to install linq.async
        //Task<RedisKey[]> KeysAsync(string pattern);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1);
        Task<HashEntry[]> HashGetAllAsync(RedisKey key);
        Task<RedisValue[]> SMembersAsync(string key);
        Task<bool> SIsMemberAsync(string key, string value);
    }
}
