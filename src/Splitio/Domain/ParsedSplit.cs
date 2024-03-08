using System.Collections.Generic;
using System.Linq;

namespace Splitio.Domain
{
    public class ParsedSplit : SplitBase
    {
        public List<ConditionWithLogic> Conditions { get; set; }
        public AlgorithmEnum Algo { get; set; }
        public int TrafficAllocationSeed { get; set; }

        public SplitView ToSplitView()
        {
            var condition = Conditions
                .Where(x => x.ConditionType == ConditionType.ROLLOUT)
                .FirstOrDefault() ?? Conditions.FirstOrDefault();

            var treatments = condition != null ? condition.Partitions.Select(y => y.Treatment).ToList() : new List<string>();

            return new SplitView
            {
                name = Name,
                killed = Killed,
                changeNumber = ChangeNumber,
                treatments = treatments,
                trafficType = TrafficTypeName,
                configs = Configurations,
                defaultTreatment = DefaultTreatment,
                sets = Sets != null ? Sets.ToList() : new List<string>()
            };
        }
    }
}
