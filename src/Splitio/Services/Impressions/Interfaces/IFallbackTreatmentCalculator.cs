using Splitio.Domain;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IFallbackTreatmentCalculator
    {
        FallbackTreatment resolve(string flagName, string label);
    }
}
