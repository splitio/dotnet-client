using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Localhost;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public class LocalhostClient : SplitClient
    {
        private const string DefaultSplitFileName = ".split";
        private const string SplitFileYml = ".yml";
        private const string SplitFileYaml = ".yaml";

        private readonly ILocalhostFileService _localhostFileService;
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly ISplitFileWatcher _splitFileWatcher;
        private readonly string _fullPath;

        public LocalhostClient(ConfigurationOptions configurationOptions) : base("localhost")
        {
            var configs = (LocalhostClientConfigurations)_configService.ReadConfig(configurationOptions, ConfigTypes.Localhost);

            _fullPath = LookupFilePath(configs.FilePath);

            if (IsYamlFile(_fullPath))
            {
                _localhostFileService = new YamlLocalhostFileService();
            }
            else
            {
                _log.Warn("Localhost mode: .split/.splits mocks will be deprecated soon in favor of YAML files, which provide more targeting power. Take a look in our documentation.");

                _localhostFileService = new LocalhostFileService();
            }

            BuildFlagSetsFilter(new HashSet<string>());

            var splits = _localhostFileService.ParseSplitFile(_fullPath);
            _featureFlagCache = new InMemorySplitCache(splits, _flagSetsFilter);

            if (configs.Polling)
            {
                var worker = new SplitPeriodicTask(_statusManager, Enums.Task.LocalhostFileWatcher, configs.FileWatcherIntervalMs);
                _splitFileWatcher = new SplitFileUpdateChecker(_localhostFileService, _featureFlagCache, worker, _fullPath);
            }
            else
            {
                var worker = new SplitPeriodicTask(_statusManager, Enums.Task.LocalhostFileWatcher, 0);
                _splitFileWatcher = new SplitFileSystemWatcher(_localhostFileService, _featureFlagCache, worker, _fullPath);
            }

            _blockUntilReadyService = new NoopBlockUntilReadyService();
            _manager = new SplitManager(_featureFlagCache, _blockUntilReadyService);
            _trafficTypeValidator = new TrafficTypeValidator(_featureFlagCache, _blockUntilReadyService);
            _evaluator = new Evaluator.Evaluator(_featureFlagCache, new Splitter(), null);
            _uniqueKeysTracker = new NoopUniqueKeysTracker();
            _impressionsCounter = new NoopImpressionsCounter();
            _impressionsObserver = new NoopImpressionsObserver();
            _impressionsManager = new ImpressionsManager(null, null, _impressionsCounter, false, ImpressionsMode.Debug, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver, false);

            BuildClientExtension();

            _splitFileWatcher.Start();
        }

        #region Public Methods
        public override bool Track(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            return true;
        }

        public override Task<bool> TrackAsync(string key, string trafficType, string eventType, double? value = null, Dictionary<string, object> properties = null)
        {
            return Task.FromResult(true);
        }

        public override void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            _splitFileWatcher.StopAsync().Wait();
            _featureFlagCache.Clear();
        }

        public override async Task DestroyAsync()
        {
            if (_statusManager.IsDestroyed()) return;

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            await _splitFileWatcher.StopAsync();
            _featureFlagCache.Clear();
        }
        #endregion

        #region Private Methods
        private static string LookupFilePath(string filePath)
        {
            filePath = filePath ?? DefaultSplitFileName;

            var filePathLowerCase = filePath.ToLower();

            if (filePathLowerCase.Equals(DefaultSplitFileName))
            {
                var home = Environment.GetEnvironmentVariable("USERPROFILE");
                filePath = Path.Combine(home, filePath);

                filePathLowerCase = filePath.ToLower();
            }

            if (!(filePathLowerCase.EndsWith(SplitFileYml) || filePathLowerCase.EndsWith(SplitFileYaml) || filePathLowerCase.EndsWith(DefaultSplitFileName) || filePathLowerCase.EndsWith(".splits")))
                throw new Exception($"Invalid extension specified for Splits mock file. Accepted extensions are \".yml\" and \".yaml\". Your specified file is {filePath}");

            if (!File.Exists(filePath))
                throw new DirectoryNotFoundException($"Split configuration not found in ${filePath} - Please review your Split file location.");

            return filePath;
        }

        private static bool IsYamlFile(string fullPath)
        {
            return fullPath.ToLower().EndsWith(SplitFileYaml) || fullPath.ToLower().EndsWith(SplitFileYml);
        }
        #endregion
    }
}
