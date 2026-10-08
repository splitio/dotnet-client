using Splitio.Commons.Domain;

namespace Splitio.Commons.Impressions.Interfaces
{
    public interface IFallbackTreatmentCalculator
    {
        FallbackTreatment resolve(string flagName, string label);
    }
}
