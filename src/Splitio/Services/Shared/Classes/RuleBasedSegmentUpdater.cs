using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Shared.Classes
{
    public class RuleBasedSegmentUpdater : IUpdater<RuleBasedSegmentDto>
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RuleBasedSegmentUpdater));

        private readonly IParser<RuleBasedSegmentDto, RuleBasedSegment> _parser;
        private readonly IRuleBasedSegmentCache _ruleBasedSegmentCache;

        public RuleBasedSegmentUpdater(IParser<RuleBasedSegmentDto, RuleBasedSegment> parser,
            IRuleBasedSegmentCache ruleBasedSegmentCache)
        {
            _parser = parser;
            _ruleBasedSegmentCache = ruleBasedSegmentCache;
        }

        public List<string> Process(List<RuleBasedSegmentDto> changes, long till)
        {
            var toAdd = new List<RuleBasedSegment>();
            var toRemove = new List<string>();
            var segmentNames = new List<string>();

            foreach (var rbsDto in changes)
            {
                var rbs = _parser.Parse(rbsDto, _ruleBasedSegmentCache);

                if (rbs == null)
                {
                    toRemove.Add(rbsDto.Name);
                    continue;
                }

                toAdd.Add(rbs);
                segmentNames.AddRange(rbs.GetSegments());
            }

            _ruleBasedSegmentCache.Update(toAdd, toRemove, till);

            if (toAdd.Count > 0)
            {
                _log.Debug($"Added Rule-based Segments: {string.Join(" - ", toAdd.Select(s => s.Name).ToList())}");
            }

            if (toRemove.Count > 0)
            {
                _log.Debug($"Deleted Rule-based Segments: {string.Join(" - ", toRemove)}");
            }

            return segmentNames;
        }
    }
}
