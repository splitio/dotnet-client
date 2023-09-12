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
            _wrapperAdapter = WrapperAdapter.Instance();
        }

        protected override Task<SplitChangesResult> FetchFromBackendAsync(long since, FetchOptions fetchOptions)
        {
            var json = File.ReadAllText(_filePath);
            var splitChangesResult = JsonConvert.DeserializeObject<SplitChangesResult>(json);

            return Task.FromResult(splitChangesResult);
        }
    }
}
