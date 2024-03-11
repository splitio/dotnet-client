using Splitio.Domain;
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
                    ConditionType = ConditionType.WHITELIST,
                    Label = Labels.UnsupportedMatcherType,
                    Partitions = new List<Partition>
                    {
                        new Partition
                        {
                            Size = 100,
                            Treatment = Constants.Gral.Control
                        }
                    },
                    Matcher = new CombiningMatcher
                    {
                        Combiner = CombinerEnum.AND,
                        Delegates = new List<AttributeMatcher>
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
