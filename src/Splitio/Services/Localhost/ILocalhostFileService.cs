using Splitio.Domain;
using System.Collections.Concurrent;

namespace Splitio.Services.Localhost
{
    public interface ILocalhostFileService
    {
        ConcurrentDictionary<string, ParsedSplit> ParseSplitFile(string filePath);
    }
}
