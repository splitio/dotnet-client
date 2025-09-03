using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class UniqueKeysTracker : TrackerComponent, IUniqueKeysTracker
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(UniqueKeysTracker));

        private readonly IFilterAdapter _filterAdapter;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;
        private readonly IImpressionsSenderAdapter _senderAdapter;
        private readonly ISplitTask _cacheLongTermCleaningTask;
        private int _keySize;

        public UniqueKeysTracker(ComponentConfig config,
            IFilterAdapter filterAdapter,
            ConcurrentDictionary<string, HashSet<string>> cache,
            IImpressionsSenderAdapter senderAdapter,
            ISplitTask mtksTask,
            ISplitTask cacheLongTermCleaningTask,
            ISplitTask sendBulkDataTask) : base(config, mtksTask, sendBulkDataTask)
        {
            _filterAdapter = filterAdapter;
            _cache = cache;
            _senderAdapter = senderAdapter;
            _cacheLongTermCleaningTask = cacheLongTermCleaningTask;
            _cacheLongTermCleaningTask.SetAction(_filterAdapter.Clear);
            _keySize = 0;
        }

        #region Public Methods
        public bool Track(string key, string featureName)
        {
            if (_filterAdapter.Contains(featureName, key)) return false;

            _filterAdapter.Add(featureName, key);

            _cache.AddOrUpdate(featureName, new HashSet<string>() { key }, (_, hashSet) =>
            {
                hashSet.Add(key);
                return hashSet;
            });
            _keySize++;
            if (_keySize >= _maxBulkSize)
            {
                _taskBulkData.Start();
            }

            return true;
        }
        #endregion

        #region Protected Methods
        protected override void StartTask()
        {
            base.StartTask();
            _cacheLongTermCleaningTask.Start();
        }

        protected override async Task StopTaskAsync()
        {
            await base.StopTaskAsync();
            await _cacheLongTermCleaningTask.StopAsync();
        }

        protected override async Task SendBulkDataAsync()
        {
            try
            {
                var uniques = new ConcurrentDictionary<string, HashSet<string>>(_cache);
                var keySize = _keySize;
                _cache.Clear();
                _keySize = 0;

                if (!uniques.Any()) return;

                var values = uniques
                    .Select(v => new Mtks(v.Key, v.Value))
                    .ToList();

                if (keySize <= _maxBulkSize)
                {
                    await _senderAdapter.RecordUniqueKeysAsync(values);
                    return;
                }
                List<Mtks> bulksFlattened = FlattenBulks(values);
                await SendAllBulks(bulksFlattened);
            }
            catch (Exception e)
            {
                _logger.Error("Exception caught sending Unique Keys.", e);
            }
        }

        protected async Task SendAllBulks(List<Mtks> bulksFlattened)
        {
            List<Mtks> bulksToSend = new List<Mtks>();
            int bulkSize = 0;
            foreach (var unique in bulksFlattened)
            {
                if ((unique.Keys.Count + bulkSize) > _maxBulkSize)
                {
                    await _senderAdapter.RecordUniqueKeysAsync(bulksToSend);
                    bulkSize = 0;
                    bulksToSend = new List<Mtks>();
                }
                bulkSize += unique.Keys.Count;
                bulksToSend.Add(unique);
            }
            await _senderAdapter.RecordUniqueKeysAsync(bulksToSend);
        }

        protected List<Mtks> FlattenBulks(List<Mtks> values)
        {
            List<Mtks> bulksFlattened = new List<Mtks>();
            foreach (var unique in values)
            {
                var bulks = ConvertToBulks(unique);
                bulksFlattened.AddRange(bulks);
            }
            return bulksFlattened;
        }

        protected List<Mtks> ConvertToBulks(Mtks unique)
        {
            if (unique.Keys.Count > _maxBulkSize)
            {
                List<Mtks> chunks = new List<Mtks>();
                var uniqueTemp = new List<String>(unique.Keys.ToArray());
                var bulks = Util.Helper.ChunkBy(uniqueTemp, _maxBulkSize);
                foreach (var bulk in bulks)
                {
                    chunks.Add(new Mtks(unique.Feature, new HashSet<string>(bulk)));
                }
                return chunks;
            }

            return new List<Mtks> { unique };
        }
        #endregion
    }
}
