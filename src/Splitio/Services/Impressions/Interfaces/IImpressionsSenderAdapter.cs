using Splitio.Services.Impressions.Classes;
using Splitio.Telemetry.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsSenderAdapter
    {
        Task RecordUniqueKeysAsync(List<Mtks> uniques);
        Task RecordImpressionsCountAsync(List<ImpressionsCountModel> values);
    }
}
