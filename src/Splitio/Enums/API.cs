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
        GetTreatmentsWithConfigByFlagSet,
        GetTreatmentsByFlagSet,
        Track,
        GetTreatmentAsync,
        GetTreatmentsAsync,
        GetTreatmentWithConfigAsync,
        GetTreatmentsWithConfigAsync,
        GetTreatmentsWithConfigByFlagSetsAsync,
        GetTreatmentsByFlagSetsAsync,
        GetTreatmentsWithConfigByFlagSetAsync,
        GetTreatmentsByFlagSetAsync,
        TrackAsync,
        
        // Manager
        Split,
        SplitAsync,

        // Matchers
        DependecyMatcher,
        DependecyMatcherAsync
    }
}

namespace Splitio.Enums.Extensions
{
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
                    return MethodEnum.TreatmentsWithConfigByFlagSets;
                case API.GetTreatmentsByFlagSets:
                case API.GetTreatmentsByFlagSetsAsync:
                    return MethodEnum.TreatmentsByFlagSets;
                case API.GetTreatmentsWithConfigByFlagSet:
                case API.GetTreatmentsWithConfigByFlagSetAsync:
                    return MethodEnum.TreatmentsWithConfigByFlagSet;
                case API.GetTreatmentsByFlagSet:
                case API.GetTreatmentsByFlagSetAsync:
                    return MethodEnum.TreatmentsByFlagSet;
                default:
                    throw new Exception("");
            }
        }
    }
}