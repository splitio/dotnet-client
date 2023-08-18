using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Splitio.Services.InputValidation.Classes
{
    public class FlagSetsValidator : IFlagSetsValidator
    {
        private const string SetExpectedRegex = "^[a-z][_a-z0-9]{1,50}$";

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FlagSetsValidator));

        public HashSet<string> Cleanup(List<string> flagSets)
        {
            var toReturn = new HashSet<string>();

            foreach (var flagSet in flagSets)
            {
                var set = flagSet.ToLower();

                if (set.StartsWith(" ") || set.EndsWith(" "))
                {
                    _log.Warn($"SDK config: Flag Set name <<{flagSet}>> has extra whitespace, trimming");

                    // Trim whitespaces and add to a set to be sure that will return unique FlagSets
                    set = set.Trim();
                }

                if (!toReturn.Add(set))
                {
                    _log.Warn($"SDK config: you passed duplicated Flag Set. {set} was deduplicated.");
                }
            }

            return toReturn;
        }

        public HashSet<string> Items(HashSet<string> flagSets)
        {
            var toReturn = new HashSet<string>();

            foreach (var set in flagSets)
            {
                if (Regex.Match(set, SetExpectedRegex).Success)
                    toReturn.Add(set);
                else
                    _log.Warn($"SDK config: you passed {set}, Flag Set must adhere to the regular expressions {SetExpectedRegex}. This means an Flag Set must be start with a letter, be in lowercase, alphanumeric and have a max length of 50 characteres. {set} was discarded.");
            }

            return toReturn;
        }
    }
}
