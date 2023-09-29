using System.Collections.Generic;

namespace Splitio.Domain
{
    public class MultipleEvaluatorResult
    {
        public MultipleEvaluatorResult() { }
        public MultipleEvaluatorResult(List<TreatmentResult> results, long elapsedMilliseconds, bool exception)
        {
            Results = results;
            ElapsedMilliseconds = elapsedMilliseconds;
            Exception = exception;
        }

        public List<TreatmentResult> Results { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public bool Exception { get; set; }
    }
}
