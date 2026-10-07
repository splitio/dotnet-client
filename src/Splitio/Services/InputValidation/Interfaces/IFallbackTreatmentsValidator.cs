using Splitio.Commons.Domain;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IFallbackTreatmentsValidator
    {
        FallbackTreatmentsConfiguration validate(FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration);
    }
}