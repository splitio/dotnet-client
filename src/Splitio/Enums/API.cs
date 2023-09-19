using Splitio.Telemetry.Domain.Enums;
using System;

namespace Splitio.Enums
{
    public enum API
    {
        // Client
        GetTreatment,
        GetTreatments,
        GetTreatmentWithConfig,
        GetTreatmentsWithConfig,
        Track,
        GetTreatmentAsync,
        GetTreatmentsAsync,
        GetTreatmentWithConfigAsync,
        GetTreatmentsWithConfigAsync,
        TrackAsync,
        
        // Manager
        Split,
        SplitAsync
    }

    public static class EnumExtensions
    {
        public static MethodEnum ConvertToMethodEnum(this API method)
        {
            switch (method)
            {
                case API.GetTreatment:
                case API.GetTreatmentAsync:
                    return MethodEnum.Treatment;
                case API.GetTreatments:
                case API.GetTreatmentsAsync:
                    return MethodEnum.Treatments;
                case API.GetTreatmentWithConfig:
                case API.GetTreatmentWithConfigAsync:
                    return MethodEnum.TreatmentWithConfig;
                case API.GetTreatmentsWithConfig:
                case API.GetTreatmentsWithConfigAsync:
                    return MethodEnum.TreatmentsWithConfig;
                case API.Track:
                case API.TrackAsync:
                    return MethodEnum.Track;
                default:
                    throw new Exception("");
            }
        }
    }
}
