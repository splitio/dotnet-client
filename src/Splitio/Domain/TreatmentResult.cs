namespace Splitio.Domain
{
    public class TreatmentResult
    {
        public string Label { get; set; }
        public string Treatment { get; set; }
        public long? ChangeNumber { get; set; }
        public string Config { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public bool Exception { get; set; }

        public TreatmentResult(string label, string treatment, long? changeNumber = null, string config = null, long? elapsedMilliseconds = null, bool exception = false)
        {
            Label = label;
            Treatment = treatment;
            ChangeNumber = changeNumber;
            Config = config;
            ElapsedMilliseconds = elapsedMilliseconds ?? 0;
            Exception = exception;
        }
    }
}