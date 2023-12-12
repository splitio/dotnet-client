using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public abstract class BaseSplitFileWatcher : ISplitFileWatcher
    {
        private readonly ILocalhostFileService _localhostFileService;
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly ISplitTask _worker;
        
        protected readonly CancellationTokenSource _cts;
        protected readonly string _fullPath;

        public BaseSplitFileWatcher(ILocalhostFileService localhostFileService,
            IFeatureFlagCache featureFlagCache,
            ISplitTask worker,
            string fullPath)
        {
            _cts = new CancellationTokenSource();
            _localhostFileService = localhostFileService;
            _featureFlagCache = featureFlagCache;
            _worker = worker;
            _worker.SetAction(Work);
            _fullPath = fullPath;
        }

        public void Start()
        {
            _worker.Start();
        }

        public async Task StopAsync()
        {
            try
            {
                _cts.Cancel();
                await _worker.StopAsync();
                _cts?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"#2: {ex.ToString()} ");
            }
        }

        protected void ProcessSplitFileUpdate()
        {
            var featureFlagsToAdd = _localhostFileService.ParseSplitFile(_fullPath);

            var namesInCache = _featureFlagCache.GetSplitNames();
            var featureFlagstoRemove = namesInCache.Except(featureFlagsToAdd.Keys).ToArray();

            _featureFlagCache.Update(featureFlagsToAdd.Values.ToList(), featureFlagstoRemove.ToList(), -1);
        }

        protected abstract void Work();
    }
}
