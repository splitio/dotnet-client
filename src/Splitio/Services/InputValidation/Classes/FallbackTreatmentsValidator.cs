using Splitio.Domain;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Splitio.Services.InputValidation.Classes
{
    public class FallbackTreatmentsValidator : IFallbackTreatmentsValidator
    {
        private const string TreatmentMatcher = "^[0-9]+[.a-zA-Z0-9_-]*$|^[a-zA-Z]+[a-zA-Z0-9_-]*$";
        private const int MaxLength = 100;

        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FallbackTreatmentsValidator));

        public FallbackTreatmentsConfiguration validate(FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration)
        {
            FallbackTreatmentsConfiguration processedFallback = new FallbackTreatmentsConfiguration();
            if (fallbackTreatmentsConfiguration == null)
                return processedFallback;

            FallbackTreatment processedGlobalFallbackTreatment = fallbackTreatmentsConfiguration.GlobalFallbackTreatment;
            Dictionary<string, FallbackTreatment> processedByFlagFallbackTreatment = processedFallback.ByFlagFallbackTreatment;

            if (fallbackTreatmentsConfiguration.GlobalFallbackTreatment != null)
            {
                processedGlobalFallbackTreatment = new FallbackTreatment(
                        IsValidTreatment(fallbackTreatmentsConfiguration.GlobalFallbackTreatment.Treatment),
                        fallbackTreatmentsConfiguration.GlobalFallbackTreatment.Config);
                if (processedGlobalFallbackTreatment.Treatment == null)
                {
                    processedGlobalFallbackTreatment = null;
                }
            }

            if (fallbackTreatmentsConfiguration.ByFlagFallbackTreatment != null)
            {
                processedByFlagFallbackTreatment = IsValidByFlagTreatment(fallbackTreatmentsConfiguration.ByFlagFallbackTreatment);
            }
            return new FallbackTreatmentsConfiguration(processedGlobalFallbackTreatment, processedByFlagFallbackTreatment);
        }

        public string IsValidTreatment(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _log.Error($"FallbackTreatments: you passed a null or empty treatment, fallback treatment must be a non-empty string");
                return null;
            }

            string trimmed = name.Trim();
            if (!trimmed.Equals(name))
            {
                _log.Warn($"FallbackTreatments: fallback treatment %s has extra whitespace, trimming");
                name = trimmed;
            }

            if (name.Length > MaxLength)
            {
                return null;
            }

            if (!Regex.IsMatch(name, TreatmentMatcher, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            {
                _log.Error($"FallbackTreatments: you passed {name}, treatment must adhere to the regular expression {TreatmentMatcher}");
                return null;
            }

            return name;
        }

        public Dictionary<string, FallbackTreatment> IsValidByFlagTreatment(Dictionary<string, FallbackTreatment> byFlagTreatment)
        {
            Dictionary<string, FallbackTreatment> result = new Dictionary<string, FallbackTreatment>();
            foreach (var entry in byFlagTreatment)
            {
                string featureName = new SplitNameValidator(_log).SplitNameIsValid(entry.Key, Enums.API.Split).Value;
                if (string.IsNullOrEmpty(featureName))
                {
                    continue;
                }

                FallbackTreatment fallbackTreatment = entry.Value;
                string treatment = IsValidTreatment(fallbackTreatment.Treatment);
                if (treatment != null)
                {
                    result.Add(featureName, new FallbackTreatment(treatment, fallbackTreatment.Config));
                }
            }

            return result;
        }
    }
}
