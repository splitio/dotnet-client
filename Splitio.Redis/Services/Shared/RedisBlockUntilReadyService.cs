using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Threading;

namespace Splitio.Redis.Services.Shared
{
    public class RedisBlockUntilReadyService : IBlockUntilReadyService
    {
        private readonly IRedisAdapter _redisAdapter;

        public RedisBlockUntilReadyService(IRedisAdapter redisAdapter)
        {
            _redisAdapter = redisAdapter;
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            if (!IsSdkReady())
            {
                var ready = false;
                using(var clock = new Util.SplitStopwatch())
                {
                    clock.Start();

                    while (clock.ElapsedMilliseconds <= blockMilisecondsUntilReady)
                    {
                        if (IsSdkReady())
                        {
                            ready = true;
                            break;
                        }

                        Thread.Sleep(500);
                    }

                    if (!ready) throw new TimeoutException($"SDK was not ready in {blockMilisecondsUntilReady}. Could not connect to Redis");
                }
            }
        }

        public bool IsSdkReady()
        {
            return _redisAdapter.IsConnected();
        }
    }
}
