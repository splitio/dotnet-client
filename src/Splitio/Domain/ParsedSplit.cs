using System.Collections.Generic;
using System.Linq;

namespace Splitio.Domain
{
    public class ParsedSplit : SplitBase
    {
        public List<ConditionWithLogic> conditions { get; set; }
        public AlgorithmEnum algo { get; set; }
        public int trafficAllocationSeed { get; set; }

        public SplitView ToSplitView()
        {
            var condition = conditions
                .Where(x => x.conditionType == ConditionType.ROLLOUT)
                .FirstOrDefault() ?? conditions.FirstOrDefault();

            var treatments = condition != null ? condition.partitions.Select(y => y.treatment).ToList() : new List<string>();

            return new SplitView
            {
                name = name,
                killed = killed,
                changeNumber = changeNumber,
                treatments = treatments,
                trafficType = trafficTypeName,
                configs = configurations,
                defaultTreatment = defaultTreatment,
                sets = Sets != null ? Sets.ToList() : new List<string>(),
                impressionsDisabled = impressionsDisabled
            };
        }
    }
}
