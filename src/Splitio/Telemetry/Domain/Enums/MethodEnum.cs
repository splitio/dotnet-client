namespace Splitio.Telemetry.Domain.Enums
{
    public enum MethodEnum
    {
        Treatment,
        Treatments,
        TreatmentWithConfig,
        TreatmentsWithConfig,
        TreatmentsWithConfigByFlagSets,
        TreatmentsByFlagSets,
        TreatmentsWithConfigByFlagSet,
        TreatmentsByFlagSet,
        Track
    }

    public static class MethodEnumExtensions
    {
        public static string GetString(this MethodEnum me)
        {
            switch (me)
            {
                case MethodEnum.Treatment:
                    return "treatment";
                case MethodEnum.Treatments:
                    return "treatments";
                case MethodEnum.TreatmentWithConfig:
                    return "treatmentWithConfig";
                case MethodEnum.TreatmentsWithConfig:
                    return "treatmentsWithConfig";
                case MethodEnum.TreatmentsWithConfigByFlagSets:
                    return "treatmentsWithConfigByFlagSets";
                case MethodEnum.TreatmentsByFlagSets:
                    return "treatmentsByFlagSets";
                case MethodEnum.TreatmentsWithConfigByFlagSet:
                    return "treatmentsWithConfigByFlagSet";
                case MethodEnum.TreatmentsByFlagSet:
                    return "treatmentsByFlagSet";
                case MethodEnum.Track:
                    return "track";
                default:
                    return "NO VALUE GIVEN";
            }
        }
    }
}