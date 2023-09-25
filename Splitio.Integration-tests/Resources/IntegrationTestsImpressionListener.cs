using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;

namespace Splitio.Integration_tests.Resources
{
    public class IntegrationTestsImpressionListener : IImpressionListener
    {
        //BlockingQueue<KeyImpression> queue;

        private readonly List<KeyImpression> _queue;

        public IntegrationTestsImpressionListener(int size)
        {
            //queue = new BlockingQueue<KeyImpression>(size);
            _queue = new List<KeyImpression>();
        }

        public void Log(KeyImpression impression)
        {
            //if (queue.HasReachedMaxSize())
            //{
            //    queue.Dequeue();
            //}

            //queue.Enqueue(impression);

            _queue.Add(impression);
        }

        public List<KeyImpression> GetQueue()
        {
            return _queue;
        }
    }
}
