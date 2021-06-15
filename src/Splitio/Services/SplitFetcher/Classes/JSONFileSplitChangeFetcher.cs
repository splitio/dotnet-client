using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class JSONFileSplitChangeFetcher : SplitChangeFetcher, ISplitChangeFetcher 
    {
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly string _filePath;

        public ISplitCache splitCache { get; private set; }        

        public JSONFileSplitChangeFetcher(string filePath)
        {
            _filePath = filePath;
            _wrapperAdapter = new WrapperAdapter();
        }

        protected override async Task<SplitChangesResult> FetchFromBackend(long since, bool cacheControlHeaders = false)
        {
            var json = File.ReadAllText(_filePath);
            var splitChangesResult = JsonConvert.DeserializeObject<SplitChangesResult>(json);

            return await _wrapperAdapter.TaskFromResult(splitChangesResult);
        }
    }
}
