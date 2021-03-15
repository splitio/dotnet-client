using System.Collections.Generic;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IMiscTelemetryConsumer
    {
        IList<string> PopTags();
    }
}
