using Splitio.Telemetry.Domain;
using System.Collections.Generic;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IStreamingTelemetryConsumer
    {
        IList<StreamingEvent> PopStreamingEvents();
    }
}
