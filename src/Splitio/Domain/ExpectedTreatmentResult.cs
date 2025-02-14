using Splitio.CommonLibraries;

namespace Splitio.Domain
{
    public class ExpectedTreatmentResult
    {
        public TreatmentResult TreatmentResult { get; set; }
        public bool ImpressionsDisabled { get; set; }

        public ExpectedTreatmentResult(TreatmentResult treatmentResult, bool impressionsDisabled)
        {
            TreatmentResult = treatmentResult;
            ImpressionsDisabled = impressionsDisabled;
        }
    }
}