﻿using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Concurrent;

namespace Splitio.Integration_tests.Resources
{
    public class IntegrationTestsImpressionListener : IImpressionListener
    {
        private readonly ConcurrentDictionary<string, KeyImpression> _queue;

        public IntegrationTestsImpressionListener(int size)
        {
            _queue = new ConcurrentDictionary<string, KeyImpression>();
        }

        public void Log(KeyImpression impression)
        {
            var key = $"{impression.feature}::{impression.keyName}";
            _queue.TryAdd(key, impression);
        }

        public KeyImpression Get(string feature, string keyName)
        {
            return _queue.TryGetValue($"{feature}::{keyName}", out var key) ? key : null;
        }

        public int Count()
        {
            return _queue.Count;
        }
    }
}
