﻿#if NET45
using Splitio.Services.Cache.Lru;
#else
using BitFaster.Caching.Lru;
#endif
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsObserver : IImpressionsObserver
    {
        private readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsObserver));

        private const int DefaultCacheSize = 500000;

#if NET45
        private readonly LruCache<ulong, long> _cache;
#else
        private readonly ConcurrentLru<ulong, long> _cache;
#endif

        private readonly IImpressionHasher _impressionHasher;

        public ImpressionsObserver(IImpressionHasher impressionHasher)
        {
            _impressionHasher = impressionHasher;
#if NET45
            _cache = new LruCache<ulong, long>(DefaultCacheSize);
#else
            _cache = new ConcurrentLru<ulong, long>(DefaultCacheSize);
#endif
        }

        public long? TestAndSet(KeyImpression impression)
        {
            long? toReturn = null;
            try
            {

                if (impression == null)
                {
                    return toReturn;
                }

                ulong hash = _impressionHasher.Process(impression);

#if NET45
                if (_cache.TryGetValue(hash, out long previous))
                {
                    toReturn = Math.Min(previous, impression.time);
                }

                _cache.AddOrUpdate(hash, impression.time);
#else
                if (_cache.TryGet(hash, out long previous))
                {
                    toReturn = Math.Min(previous, impression.time);
                }

                _cache.AddOrUpdate(hash, impression.time);
#endif
            }
            catch (Exception ex)
            {
                _logger.Warn("Something went wrong in Impression Observer.", ex);
            }

            return toReturn;
        }
    }
}
