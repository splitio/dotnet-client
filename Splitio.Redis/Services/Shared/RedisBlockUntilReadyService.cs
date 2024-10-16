using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Threading;

namespace Splitio.Redis.Services.Shared
{
    public class RedisBlockUntilReadyService : IBlockUntilReadyService
    {
        private readonly IRedisAdapterConsumer _redisAdapterConsumer;

        public RedisBlockUntilReadyService(IRedisAdapterConsumer redisAdapterConsumer)
        {
            _redisAdapterConsumer = redisAdapterConsumer;
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
            return _redisAdapterConsumer.IsConnected();
        }
    }
}
