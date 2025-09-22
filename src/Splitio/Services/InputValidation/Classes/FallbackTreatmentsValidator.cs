using Splitio.Domain;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Splitio.Services.InputValidation.Classes
{
    public class FallbackTreatmentsValidator : IFallbackTreatmentsValidator
    {
        private const string TreatmentMatcher = "^[0-9]+[.a-zA-Z0-9_-]*$|^[a-zA-Z]+[a-zA-Z0-9_-]*$";
        private const int MaxLength = 100;

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FallbackTreatmentsValidator));

        public FallbackTreatmentsConfiguration validate(FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration, Enums.API method)
        {
            FallbackTreatmentsConfiguration processedFallback = new FallbackTreatmentsConfiguration(null, null);
            if (fallbackTreatmentsConfiguration == null)
                return processedFallback;

            FallbackTreatment processedGlobalFallbackTreatment = fallbackTreatmentsConfiguration.GlobalFallbackTreatment;
            Dictionary<string, FallbackTreatment> processedByFlagFallbackTreatment = processedFallback.ByFlagFallbackTreatment;

            if (fallbackTreatmentsConfiguration.GlobalFallbackTreatment != null)
            {
                processedGlobalFallbackTreatment = new FallbackTreatment(
                        IsValidTreatment(fallbackTreatmentsConfiguration.GlobalFallbackTreatment.Treatment, method),
                        fallbackTreatmentsConfiguration.GlobalFallbackTreatment.Config);
            }

            if (fallbackTreatmentsConfiguration.ByFlagFallbackTreatment != null)
            {
                processedByFlagFallbackTreatment = IsValidByFlagTreatment(fallbackTreatmentsConfiguration.ByFlagFallbackTreatment, method);
            }
            return new FallbackTreatmentsConfiguration(processedGlobalFallbackTreatment, processedByFlagFallbackTreatment);
        }

        public string IsValidTreatment(string name, Enums.API method)
        {
            if (string.IsNullOrEmpty(name))
            {
                _log.Error($"{method}: you passed a null or empty treatment, fallback treatment must be a non-empty string");
                return null;
            }

            string trimmed = name.Trim();
            if (!trimmed.Equals(name))
            {
                _log.Warn($"{method}: fallback treatment %s has extra whitespace, trimming");
                name = trimmed;
            }

            if (name.Length > MaxLength)
            {
                return null;
            }

            if ((!Regex.Match(name, TreatmentMatcher).Success))
            {
                _log.Error($"{method}: you passed {name}, treatment must adhere to the regular expression {TreatmentMatcher}");
                return null;
            }

            return name;
        }

        public Dictionary<string, FallbackTreatment> IsValidByFlagTreatment(Dictionary<string, FallbackTreatment> byFlagTreatment, Enums.API method)
        {
            Dictionary<string, FallbackTreatment> result = new Dictionary<string, FallbackTreatment>();
            foreach (var entry in byFlagTreatment)
            {
                string featureName = new SplitNameValidator(_log).SplitNameIsValid(entry.Key, method).Value;
                if (string.IsNullOrEmpty(featureName))
                {
                    continue;
                }

                FallbackTreatment fallbackTreatment = entry.Value;
                string treatment = IsValidTreatment(fallbackTreatment.Treatment, method);
                if (treatment != null)
                {
                    result.Add(featureName, new FallbackTreatment(treatment, fallbackTreatment.Config));
                }
            }

            return result;
        }
    }
}
