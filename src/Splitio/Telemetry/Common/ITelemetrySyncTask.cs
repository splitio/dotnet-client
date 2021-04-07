using System;
using System.Collections.Generic;
using System.Text;

namespace Splitio.Telemetry.Common
{
    public interface ITelemetrySyncTask
    {
        void Start();
        void Stop();
    }
}
