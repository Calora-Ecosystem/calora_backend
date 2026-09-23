using Core.Enums;

namespace Core.Helpers;

/// <summary>
/// Qadamdan masofa va yoqilgan kkal hisoblash formulasi (<c>users/steps/metrics</c>).
/// </summary>
public static class StepMetricsHelper
{
    private const double MPerKm = 1000;

    /// <summary>Qadam uzunligi, metr.</summary>
    public static double StrideM(EnumGender gender) => gender == EnumGender.Male ? 0.8 : 0.7;

    private static double KcalFactor(EnumGender gender) => gender == EnumGender.Male ? 1.06 : 0.98;

    /// <summary>Masofa, metr.</summary>
    public static double DistanceM(EnumGender gender, double steps) => StrideM(gender) * steps;

    /// <summary>Bitta qadamda yoqiladigan kkal.</summary>
    public static double KcalPerStep(double weight, EnumGender gender) =>
        weight * (StrideM(gender) / MPerKm) * KcalFactor(gender);
}
