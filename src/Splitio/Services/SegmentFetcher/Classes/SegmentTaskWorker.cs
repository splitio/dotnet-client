﻿using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentTaskWorker : IQueueObserver
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SegmentTaskWorker));
        
        private readonly int _numberOfParallelTasks;
        private readonly SplitQueue<SelfRefreshingSegment> _queue;

        public SegmentTaskWorker(int numberOfParallelTasks,
            SplitQueue<SelfRefreshingSegment> queue)
        {
            _numberOfParallelTasks = 2;
            _queue = queue;
        }

        public async Task Notify()
        {
            var semaphore = new SemaphoreSlim(_numberOfParallelTasks, _numberOfParallelTasks);
            var tasks = new List<Task>();

            while (_queue.TryDequeue(out SelfRefreshingSegment segment))
            {
                await semaphore.WaitAsync(); // Wait until there's room for another task to run
                tasks.Add(RunTaskAsync(segment, semaphore));
            }

            await Task.WhenAll(tasks);
        }

        private async Task RunTaskAsync(SelfRefreshingSegment segment, SemaphoreSlim semaphore)
        {
            try
            {
                await segment.FetchSegmentAsync(new FetchOptions());
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
