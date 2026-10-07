using Splitio.Commons.Dto;
using System.Collections.Generic;

namespace Splitio.Services.EngineEvaluator
{
    public interface ISplitter
    {
        string GetTreatment(string key, int seed, List<PartitionDefinition> partitions, AlgorithmEnum algorithm);
        int GetBucket(string key, int seed, AlgorithmEnum algorithm);
    }
}
