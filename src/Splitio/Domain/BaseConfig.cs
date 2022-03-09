using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Domain
{
    public class BaseConfig
    {
        public string SdkVersion { get; set; }
        public string SdkMachineName { get; set; }
        public string SdkMachineIP { get; set; }
        public bool LabelsEnabled { get; set; }
        public ImpressionsMode ImpressionsMode { get; set; }

        // Bloom Filter
        public int BfExpectedElements { get; set; }
        public double BfErrorRate { get; set; }

        // Rates
        public int UniqueKeysRefreshRate { get; set; }
        public int ImpressionsCounterRefreshRate { get; set; }

        // Cache Max Size Allowed
        public int UniqueKeysCacheMaxSize { get; set; }
    }
}
