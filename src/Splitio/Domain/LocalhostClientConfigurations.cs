namespace Splitio.Domain
{
    public class LocalhostClientConfigurations : BaseConfig
    {
        public int FileWatcherIntervalMs { get; set; }
        public string FilePath { get; set; }
        public bool Polling { get; set; }
    }
}
