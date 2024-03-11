using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Parsing;
using Splitio.Services.Shared.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Localhost
{
    public abstract class AbstractLocalhostFileService : ILocalhostFileService
    {
        protected const string Control = "control";

        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger("LocalhostFileService");

        public abstract ConcurrentDictionary<string, ParsedSplit> ParseSplitFile(string filePath);

        protected static ParsedSplit CreateParsedSplit(string name, string treatment, List<ConditionWithLogic> codnitions = null)
        {
            var split = new ParsedSplit()
            {
                Name = name,
                Seed = 0,
                DefaultTreatment = treatment,
                Conditions = codnitions,
                Algo = AlgorithmEnum.Murmur,
                TrafficAllocation = 100
            };

            return split;
        }

        protected static ConditionWithLogic CreateCondition(string treatment, List<string> keys = null)
        {
            if (keys != null)
            {
                return new ConditionWithLogic
                {
                    ConditionType = ConditionType.WHITELIST,
                    Matcher = new CombiningMatcher
                    {
                        Combiner = CombinerEnum.AND,
                        Delegates = new List<AttributeMatcher>
                        {
                            new AttributeMatcher
                            {
                                negate = false,
                                matcher = new WhitelistMatcher(keys)
                            }
                        }
                    },
                    Partitions = new List<Partition>
                    {
                        new Partition
                        {
                            Size = 100,
                            Treatment = treatment
                        }
                    },
                    Label = $"whitelisted {string.Join(", ", keys)}"
                };
            }

            return new ConditionWithLogic
            {
                ConditionType = ConditionType.ROLLOUT,
                Matcher = new CombiningMatcher
                {
                    Combiner = CombinerEnum.AND,
                    Delegates = new List<AttributeMatcher>
                    {
                        new AttributeMatcher
                        {
                            negate = false,
                            matcher = new AllKeysMatcher()
                        }
                    }
                },
                Partitions = new List<Partition>
                {
                    new Partition
                    {
                        Size = 100,
                        Treatment = treatment
                    }
                },
                Label = "Default rule"
            };
        }
    }
}
