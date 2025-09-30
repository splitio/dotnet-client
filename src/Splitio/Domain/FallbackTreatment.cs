namespace Splitio.Domain
{
    public class FallbackTreatment
    {
        public FallbackTreatment(string treatment)
        {
            Treatment = treatment;
            Config = null;
            Label = null;
        }

        public FallbackTreatment(string treatment, string config)
        {
            Treatment = treatment;
            Config = config;
            Label = null;
        }

        public FallbackTreatment(string treatment, string config, string label)
        {
            Treatment = treatment;
            Config = config;
            Label = label;
        }

        public string Config { get; set; }
        public string Treatment { get; set; }
        public string Label { get; set; }
    }
}
