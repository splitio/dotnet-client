using Splitio.Services.Filters;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Splitio.Services.InputValidation.Classes
{
    public class FlagSetsValidator : IFlagSetsValidator
    {
        private const string SetExpectedRegex = "^[a-z0-9][_a-z0-9]{0,49}$";

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FlagSetsValidator));

        public HashSet<string> Cleanup(string method, List<string> flagSets)
        {
            var toReturn = new HashSet<string>();

            foreach (var flagSet in flagSets)
            {
                var set = flagSet.ToLower();
                var trimed = set.Trim();

                if (set.Length != trimed.Length)
                {
                    _log.Warn($"{method}: Flag Set name <<{flagSet}>> has extra whitespace, trimming");

                    // Trim whitespaces and add to a set to be sure that will return unique FlagSets
                    set = trimed;
                }

                if (!toReturn.Add(set))
                {
                    _log.Warn($"{method}: you passed duplicated Flag Set. {set} was deduplicated.");
                }
            }

            return toReturn;
        }

        public HashSet<string> Items(string method, HashSet<string> flagSets, IFlagSetsFilter service = null)
        {
            var toReturn = new HashSet<string>();

            foreach (var set in flagSets)
            {
                if (!Regex.Match(set, SetExpectedRegex).Success)
                {
                    _log.Warn($"{method}: you passed {set}, Flag Set must adhere to the regular expressions {SetExpectedRegex}. This means an Flag Set must be start with a letter, be in lowercase, alphanumeric and have a max length of 50 characteres. {set} was discarded.");
                    continue;
                }

                if (service != null && !service.Match(set))
                {
                    _log.Warn($"{method}: you passed {set} wich is not part of the configured FlagSetsFilter, ignoring the request.");
                    continue;
                }

                toReturn.Add(set);
            }

            return toReturn;
        }

        public bool AreValid(string method, List<string> flagSets, IFlagSetsFilter service, out HashSet<string> setsToReturn)
        {
            setsToReturn = null;

            if (flagSets == null || flagSets.Count == 0)
            {
                _log.Error($"{method}: FlagSets must be a non-empty list.");

                return false;
            }

            var sets = Cleanup(method, flagSets);
            setsToReturn = Items(method, sets, service);

            return setsToReturn != null && setsToReturn.Count > 0;
        }
    }
}
