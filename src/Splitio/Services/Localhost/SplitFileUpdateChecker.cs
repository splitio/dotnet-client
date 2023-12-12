using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.IO;
using System.Threading;

namespace Splitio.Services.Localhost
{
    public class SplitFileUpdateChecker : BaseSplitFileWatcher
    {
        private DateTime? _fileLastUpdatedDateTime;

        public SplitFileUpdateChecker(ILocalhostFileService localhostFileService,
            IFeatureFlagCache featureFlagCache,
            ISplitTask worker,
            string fullPath) : base(localhostFileService, featureFlagCache, worker, fullPath)
        { }

        protected override void Work()
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(_fullPath))
                {
                    _log.Debug($"File {_fullPath} does not exist.");
                    return;
                }

                var lastUpdated = File.GetLastWriteTime(_fullPath);

                if (_fileLastUpdatedDateTime == lastUpdated)
                {
                    _log.Debug($"File {_fullPath} was last updated at {_fileLastUpdatedDateTime}");
                    return;
                }

                _fileLastUpdatedDateTime = lastUpdated;

                ProcessSplitFileUpdate();
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException) return;

                _log.Warn("Somenting went wrong processing SplitFileUpdate.", ex);
            }
        }
    }
}
