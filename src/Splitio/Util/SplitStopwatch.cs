using System;
using System.Diagnostics;

namespace Splitio.Util
{
    public class SplitStopwatch : IDisposable
    {
        private readonly bool _disposed = false;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
