namespace Splitio.Telemetry.Domain.Enums
{
    public enum MethodEnum
    {
        Treatment,
        Treatments,
        TreatmentWithConfig,
        TreatmentsWithConfig,
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
                case MethodEnum.Track:
                    return "track";
                default:
                    return "NO VALUE GIVEN";
            }
        }
    }
}