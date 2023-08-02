using System.Collections.Generic;

namespace Splitio.Domain
{
    public class MultipleEvaluatorResult
    {
        public MultipleEvaluatorResult() { }
        public MultipleEvaluatorResult(Dictionary<string, TreatmentResult> results, long elapsedMilliseconds, bool exception)
        {
            TreatmentResults = results;
            ElapsedMilliseconds = elapsedMilliseconds;
            Exception = exception;
        }

        public Dictionary<string, TreatmentResult> TreatmentResults { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public bool Exception { get; set; }
    }
}
