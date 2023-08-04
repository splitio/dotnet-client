namespace Splitio.Domain
{
    public class SplitResult
    {
        public SplitResult() { }

        public SplitResult(string treatment, string config)
        {
            Treatment = treatment;
            Config = config;
        }

        public string Treatment { get; set; }
        public string Config { get; set; }
    }
}
