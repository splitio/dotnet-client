using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Services.Impressions.Classes
{
    public class FallbackTreatmentCalculator : IFallbackTreatmentCalculator
    {
        readonly FallbackTreatmentsConfiguration FallbackTreatmentsConfiguration;
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

        private static string resolveLabel(string label)
        {
            if (label == null)
            {
                return null;
            }
            return labelPrefix + label;
        }

        private static FallbackTreatment copyWithLabel(FallbackTreatment fallbackTreatment, string label)
        {
            return new FallbackTreatment(fallbackTreatment.Treatment, fallbackTreatment.Config, label);
        }
    }
}
