using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System;

namespace Splitio.Services.Impressions.Classes
{
    public class FallbackTreatmentCalculator : IFallbackTreatmentCalculator
    {
        FallbackTreatmentsConfiguration FallbackTreatmentsConfiguration;
        const string labelPrefix = "fallback - ";

        public FallbackTreatmentCalculator(FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration)
        {
            FallbackTreatmentsConfiguration = fallbackTreatmentsConfiguration;
        }

        public FallbackTreatment resolve(string flagName, string label)
        {
            if (FallbackTreatmentsConfiguration != null)
            {
                if (FallbackTreatmentsConfiguration.ByFlagFallbackTreatment != null)
                {
                    FallbackTreatment byFlackfallbackTreatment;
                    FallbackTreatmentsConfiguration.ByFlagFallbackTreatment.TryGetValue(flagName, out byFlackfallbackTreatment);
                    if (byFlackfallbackTreatment != null)
                    {
                        return copyWithLabel(byFlackfallbackTreatment, resolveLabel(label));
                    }
                }

                if (FallbackTreatmentsConfiguration.GlobalFallbackTreatment != null)
                {
                    return copyWithLabel(FallbackTreatmentsConfiguration.GlobalFallbackTreatment,
                            resolveLabel(label));
                }
            }

            return new FallbackTreatment(Constants.Gral.Control, null, label);
        }

        private String resolveLabel(String label)
        {
            if (label == null)
            {
                return null;
            }
            return labelPrefix + label;
        }

        private FallbackTreatment copyWithLabel(FallbackTreatment fallbackTreatment, String label)
        {
            return new FallbackTreatment(fallbackTreatment.Treatment, fallbackTreatment.Config, label);
        }
    }
}
