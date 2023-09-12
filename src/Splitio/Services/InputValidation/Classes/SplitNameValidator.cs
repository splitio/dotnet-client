using Splitio.Domain;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.InputValidation.Classes
{
    public class SplitNameValidator : ISplitNameValidator
    {
        private const string WHITESPACE = " ";

        protected readonly ISplitLogger _log;

        public SplitNameValidator(ISplitLogger log = null)
        {
            _log = log ?? WrapperAdapter.Instance().GetLogger(typeof(SplitNameValidator));
        }

        public List<string> SplitNamesAreValid(List<string> featureFlagNames, string method)
        {
            if (featureFlagNames == null)
            {
                _log.Error($"{method}: featureFlagNames must be a non-empty array");
                return featureFlagNames;
            }

            if (!featureFlagNames.Any())
            {
                _log.Error($"{method}: featureFlagNames must be a non-empty array");
                return featureFlagNames;
            }

            var ffNames = new HashSet<string>();

            foreach (var name in featureFlagNames)
            {
                var splitNameResult = SplitNameIsValid(name, method);

                if (splitNameResult.Success)
                {
                    ffNames.Add(splitNameResult.Value);
                }
            }

            return ffNames.ToList();
        }

        public ValidatorResult SplitNameIsValid(string featureFlagName, string method)
        {
            if (featureFlagName == null)
            {
                _log.Error($"{method}: you passed a null featureFlagName, flag name must be a non-empty string");
                return new ValidatorResult { Success = false };
            }

            if (featureFlagName == string.Empty)
            {
                _log.Error($"{method}: you passed an empty featureFlagName, flag name must be a non-empty string");
                return new ValidatorResult { Success = false };
            }

            featureFlagName = CheckWhiteSpaces(featureFlagName, method);

            return new ValidatorResult { Success = true, Value = featureFlagName };
        }

        private string CheckWhiteSpaces(string featureFlagName, string method)
        {
            if (featureFlagName.StartsWith(WHITESPACE) || featureFlagName.EndsWith(WHITESPACE))
            {
                _log.Warn($"{method}: feature flag name {featureFlagName} has extra whitespace, trimming");

                featureFlagName = featureFlagName.TrimStart();
                featureFlagName = featureFlagName.TrimEnd();
            }

            return featureFlagName;
        }
    }
}
