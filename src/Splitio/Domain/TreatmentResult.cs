using Splitio.CommonLibraries;

namespace Splitio.Domain
{
    public class TreatmentResult
    {
        public string FeatureFlagName { get; set; }
        public string Label { get; set; }
        public string Treatment { get; set; }
        public long? ChangeNumber { get; set; }
        public string Config { get; set; }
        public bool Exception { get; set; }
        public long ImpTime { get; set; }

        public TreatmentResult(string featureFlagName, string label, string treatment, long? changeNumber = null, string config = null, bool exception = false, long? impTime = null)
        {
            FeatureFlagName = featureFlagName;
            Label = label;
            Treatment = treatment;
            ChangeNumber = changeNumber;
            Config = config;
            Exception = exception;
            ImpTime = impTime ?? CurrentTimeHelper.CurrentTimeMillis();
        }
    }
}