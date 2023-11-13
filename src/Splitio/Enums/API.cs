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
        GetTreatmentsWithConfigByFlagSets,
        GetTreatmentsByFlagSets,
        Track,
        GetTreatmentAsync,
        GetTreatmentsAsync,
        GetTreatmentWithConfigAsync,
        GetTreatmentsWithConfigAsync,
        GetTreatmentsWithConfigByFlagSetsAsync,
        GetTreatmentsByFlagSetsAsync,
        TrackAsync,
        
        // Manager
        Split,
        SplitAsync,

        // Matchers
        DependecyMatcher,
        DependecyMatcherAsync
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
                case API.GetTreatmentsWithConfigByFlagSets:
                case API.GetTreatmentsWithConfigByFlagSetsAsync:
                    return MethodEnum.Treatments; // TODO: update this with telemetry implementation
                case API.GetTreatmentsByFlagSets:
                case API.GetTreatmentsByFlagSetsAsync:
                    return MethodEnum.Treatments; // TODO: update this with telemetry implementation
                default:
                    throw new Exception("");
            }
        }
    }
}
