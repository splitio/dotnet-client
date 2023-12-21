namespace Splitio.Services.Localhost
{
    public static class LocalhostFileSync
    {
        public static ILocalhostFileSync FileSyncPolling(int intervalMs) => new FileSyncPolling(intervalMs);
    }
}
