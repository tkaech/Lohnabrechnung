using Payroll.Domain.Settings;

namespace Payroll.Application.Settings;

public static class PayrollPreviewHelpCatalog
{
    public const string BaseSalaryCode = "BASE_SALARY";
    public const string TimeSupplementCode = "TIME_SUPPLEMENTS";
    public const string SpecialSupplementCode = "SPECIAL_SUPPLEMENT";
    public const string VehiclePauschalzone1Code = "VEHICLE_P1";
    public const string VehiclePauschalzone2Code = "VEHICLE_P2";
    public const string VehicleRegiezone1Code = "VEHICLE_R1";
    public const string SubtotalCode = "SUBTOTAL";
    public const string VacationCompensationCode = "VACATION_COMPENSATION";
    public const string AhvGrossCode = "AHV_GROSS";
    public const string AhvIvEoCode = "AHV_IV_EO";
    public const string AlvCode = "ALV";
    public const string KtgUvgCode = "KTG_UVG";
    public const string TrainingAndHolidayCode = "TRAINING_AND_HOLIDAY";
    public const string BvgCode = "BVG";
    public const string TotalCode = "TOTAL";
    public const string ExpensesCode = "EXPENSES";
    public const string TotalPayoutCode = "TOTAL_PAYOUT";

    private static readonly PayrollPreviewHelpOptionDto[] DefaultOptions =
    [
        new(TimeSupplementCode, "Stunden mit Zeitzuschlag", true, string.Empty),
        new(SpecialSupplementCode, "Spezialzuschlag gemaess Vertrag", true, string.Empty),
        new(SubtotalCode, "Zwischentotal", true, string.Empty),
        new(VacationCompensationCode, "Ferienentschaedigung", true, string.Empty),
        new(AhvGrossCode, "AHV-pflichtiger Bruttolohn", true, string.Empty),
        new(BvgCode, "BVG", true, string.Empty),
        new(TotalCode, "Total", true, string.Empty),
        new(TotalPayoutCode, "Total Auszahlung", true, string.Empty)
    ];

    public static IReadOnlyList<PayrollPreviewHelpOptionDto> GetDefaultOptions()
    {
        return DefaultOptions
            .Select(option => option with { })
            .ToArray();
    }

    public static IReadOnlyList<PayrollPreviewHelpOptionDto> MergeWithDefaults(
        IEnumerable<PayrollPreviewHelpVisibility>? storedOptions)
    {
        var states = (storedOptions ?? [])
            .Where(option => !string.IsNullOrWhiteSpace(option.Code))
            .GroupBy(option => option.Code, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

        return DefaultOptions
            .Select(option => option with
            {
                IsEnabled = states.TryGetValue(option.Code, out var storedOption)
                    ? storedOption.IsEnabled
                    : true,
                HelpText = states.TryGetValue(option.Code, out storedOption)
                    ? storedOption.HelpText ?? string.Empty
                    : string.Empty
            })
            .ToArray();
    }

    public static IReadOnlyList<PayrollPreviewHelpVisibility> ToDomain(
        IEnumerable<PayrollPreviewHelpOptionDto>? options)
    {
        var submittedStates = (options ?? [])
            .Where(option => !string.IsNullOrWhiteSpace(option.Code))
            .GroupBy(option => option.Code, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().IsEnabled, StringComparer.Ordinal);

        return DefaultOptions
            .Select(option => new PayrollPreviewHelpVisibility(
                option.Code,
                submittedStates.TryGetValue(option.Code, out var isEnabled)
                    ? isEnabled
                    : true,
                options?.FirstOrDefault(item => item.Code == option.Code)?.HelpText?.Trim() ?? string.Empty))
            .ToArray();
    }
}
