using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;

namespace Splitio.Integration_tests.Resources
{
    public class IntegrationTestsImpressionListener : IImpressionListener
    {
        private readonly Dictionary<string, KeyImpression> _queue;

        public IntegrationTestsImpressionListener(int size)
        {
            _queue = new Dictionary<string, KeyImpression>();
        }

        public void Log(KeyImpression impression)
        {
            _queue.Add($"{impression.feature}::{impression.keyName}", impression);
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
