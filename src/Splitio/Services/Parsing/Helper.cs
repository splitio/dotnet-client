using Splitio.Domain;
using Splitio.Commons.Dto;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Parsing.Classes
{
    public static class Helper
    {
        public static List<ConditionWithLogic> GetDefaultConditions()
        {
            return new List<ConditionWithLogic>
            {
                new ConditionWithLogic()
                {
                    conditionType = ConditionType.WHITELIST,
                    label = Labels.UnsupportedMatcherType,
                    partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = Commons.Shared.Constants.Gral.Control
                        }
                    },
                    matcher = new CombiningMatcher
                    {
                        combiner = CombinerEnum.AND,
                        delegates = new List<AttributeMatcher>
                        {
                            new AttributeMatcher
                            {
                                matcher = new AllKeysMatcher(),
                            }
                        }
                    }
                }
            };
        }

        public static CombinerEnum ParseCombiner(string combinerEnum)
        {
            _ = Enum.TryParse(combinerEnum, out CombinerEnum result);

            return result;
        }
    }
}
