using System.Collections.Generic;

namespace Splitio.Commons.Dto
{
    public class Split : SplitBase
    {        
        public string status { get; set; }
        public List<ConditionDefinition> conditions { get; set; }
        public int? algo { get; set; }
        public int? trafficAllocationSeed { get; set; }
        public List<PrerequisitesDto> Prerequisites { get; set; }
    }
}
