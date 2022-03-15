using Splitio.Services.Impressions.Classes;
using Splitio.Telemetry.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsSenderAdapter
    {
        void RecordUniqueKeys(List<Mtks> uniques);
        void RecordImpressionsCount(List<ImpressionsCountModel> values);
    }
}
