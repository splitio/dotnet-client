using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Parsing;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Classes
{
    public abstract class AbstractLocalhostFileService : ILocalhostFileService
    {
        protected const string Control = "control";

        protected ISplitLogger _log;

        public abstract ConcurrentDictionary<string, ParsedSplit> ParseSplitFile(string filePath);

        protected ParsedSplit CreateParsedSplit(string name, string treatment, List<ConditionWithLogic> codnitions = null)
        {
            var split = new ParsedSplit()
            {
                name = name,
                seed = 0,
                defaultTreatment = treatment,
                conditions = codnitions,
                algo = AlgorithmEnum.Murmur,
                trafficAllocation = 100
            };

            return split;
        }

        protected ConditionWithLogic CreateCondition(string treatment, List<string> keys = null)
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
                    Partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = treatment
                        }
                    },
                    Label = $"whitelisted {string.Join(", ", keys)}"
                };
            }
            else
            {
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
                    Partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = treatment
                        }
                    },
                    Label = "Default rule"
                };
            }
        }
    }
}
