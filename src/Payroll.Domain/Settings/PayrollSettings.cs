using Payroll.Domain.Common;
using Payroll.Domain.Employees;

namespace Payroll.Domain.Settings;

public sealed class PayrollSettings : AuditableEntity
{
    public const int VacationCompensationAge50ThresholdYears = 50;
    public const decimal DefaultAhvIvEoRate = 0.053m;
    public const decimal DefaultAlvRate = 0.011m;
    public const decimal DefaultSicknessAccidentInsuranceRate = 0.00821m;
    public const decimal DefaultTrainingAndHolidayRate = 0.00015m;
    public const decimal DefaultVacationCompensationRate = 0.1064m;
    public const decimal DefaultVacationCompensationRateAge50Plus = 0.1064m;
    public const string DefaultAppFontFamily = "Segoe UI, DejaVu Sans, Arial";
    public const decimal DefaultAppFontSize = 13m;
    public const string DefaultAppTextColorHex = "#FF1A2530";
    public const string DefaultAppMutedTextColorHex = "#FF5F6B7A";
    public const string DefaultAppBackgroundColorHex = "#FFF5F7FA";
    public const string DefaultAppAccentColorHex = "#FF14324A";
    public const string DefaultAppLogoText = "PA";
    public const string DefaultPrintFontFamily = "Helvetica";
    public const decimal DefaultPrintFontSize = 9m;
    public const string DefaultPrintTextColorHex = "#FF000000";
    public const string DefaultPrintMutedTextColorHex = "#FF4B5563";
    public const string DefaultPrintAccentColorHex = "#FFFFFF00";
    public const string DefaultPrintLogoText = "PA";
    public const string DefaultPrintTemplate = "";
    public const string DefaultDecimalSeparator = ",";
    public const string DefaultThousandsSeparator = "'";
    public const string DefaultCurrencyCode = "CHF";
    public const string DefaultPayrollPreviewHelpVisibilityJson = "";

    private PayrollSettings()
    {
        CompanyAddress = string.Empty;
        AppFontFamily = DefaultAppFontFamily;
        AppFontSize = DefaultAppFontSize;
        AppTextColorHex = DefaultAppTextColorHex;
        AppMutedTextColorHex = DefaultAppMutedTextColorHex;
        AppBackgroundColorHex = DefaultAppBackgroundColorHex;
        AppAccentColorHex = DefaultAppAccentColorHex;
        AppLogoText = DefaultAppLogoText;
        AppLogoPath = string.Empty;
        PrintFontFamily = DefaultPrintFontFamily;
        PrintFontSize = DefaultPrintFontSize;
        PrintTextColorHex = DefaultPrintTextColorHex;
        PrintMutedTextColorHex = DefaultPrintMutedTextColorHex;
        PrintAccentColorHex = DefaultPrintAccentColorHex;
        PrintLogoText = DefaultPrintLogoText;
        PrintLogoPath = string.Empty;
        PrintTemplate = DefaultPrintTemplate;
        DecimalSeparator = DefaultDecimalSeparator;
        ThousandsSeparator = DefaultThousandsSeparator;
        CurrencyCode = DefaultCurrencyCode;
        PayrollPreviewHelpVisibilityJson = DefaultPayrollPreviewHelpVisibilityJson;
        WorkTimeSupplementSettings = WorkTimeSupplementSettings.Empty;
        AhvIvEoRate = DefaultAhvIvEoRate;
        AlvRate = DefaultAlvRate;
        SicknessAccidentInsuranceRate = DefaultSicknessAccidentInsuranceRate;
        TrainingAndHolidayRate = DefaultTrainingAndHolidayRate;
        VacationCompensationRate = DefaultVacationCompensationRate;
        VacationCompensationRateAge50Plus = DefaultVacationCompensationRateAge50Plus;
    }

    public PayrollSettings(
        string? companyAddress = null,
        WorkTimeSupplementSettings? workTimeSupplementSettings = null,
        decimal ahvIvEoRate = DefaultAhvIvEoRate,
        decimal alvRate = DefaultAlvRate,
        decimal sicknessAccidentInsuranceRate = DefaultSicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate = DefaultTrainingAndHolidayRate,
        decimal vacationCompensationRate = DefaultVacationCompensationRate,
        decimal vacationCompensationRateAge50Plus = DefaultVacationCompensationRateAge50Plus,
        decimal vehiclePauschalzone1RateChf = 0m,
        decimal vehiclePauschalzone2RateChf = 0m,
        decimal vehicleRegiezone1RateChf = 0m)
    {
        CompanyAddress = NormalizeCompanyAddress(companyAddress);
        UpdateVisualSettings(
            DefaultAppFontFamily,
            DefaultAppFontSize,
            DefaultAppTextColorHex,
            DefaultAppMutedTextColorHex,
            DefaultAppBackgroundColorHex,
            DefaultAppAccentColorHex,
            DefaultAppLogoText,
            string.Empty,
            DefaultPrintFontFamily,
            DefaultPrintFontSize,
            DefaultPrintTextColorHex,
            DefaultPrintMutedTextColorHex,
            DefaultPrintAccentColorHex,
            DefaultPrintLogoText,
            string.Empty);
        UpdateDecimalSeparator(DefaultDecimalSeparator);
        UpdateThousandsSeparator(DefaultThousandsSeparator);
        UpdateCurrencyCode(DefaultCurrencyCode);
        WorkTimeSupplementSettings = workTimeSupplementSettings ?? WorkTimeSupplementSettings.Empty;
        UpdateDeductionAndVehicleRates(
            ahvIvEoRate,
            alvRate,
            sicknessAccidentInsuranceRate,
            trainingAndHolidayRate,
            vacationCompensationRate,
            vacationCompensationRateAge50Plus,
            vehiclePauschalzone1RateChf,
            vehiclePauschalzone2RateChf,
            vehicleRegiezone1RateChf);
    }

    public WorkTimeSupplementSettings WorkTimeSupplementSettings { get; private set; }
    public string CompanyAddress { get; private set; }
    public string AppFontFamily { get; private set; } = string.Empty;
    public decimal AppFontSize { get; private set; }
    public string AppTextColorHex { get; private set; } = string.Empty;
    public string AppMutedTextColorHex { get; private set; } = string.Empty;
    public string AppBackgroundColorHex { get; private set; } = string.Empty;
    public string AppAccentColorHex { get; private set; } = string.Empty;
    public string AppLogoText { get; private set; } = string.Empty;
    public string AppLogoPath { get; private set; } = string.Empty;
    public string PrintFontFamily { get; private set; } = string.Empty;
    public decimal PrintFontSize { get; private set; }
    public string PrintTextColorHex { get; private set; } = string.Empty;
    public string PrintMutedTextColorHex { get; private set; } = string.Empty;
    public string PrintAccentColorHex { get; private set; } = string.Empty;
    public string PrintLogoText { get; private set; } = string.Empty;
    public string PrintLogoPath { get; private set; } = string.Empty;
    public string PrintTemplate { get; private set; } = string.Empty;
    public string DecimalSeparator { get; private set; } = DefaultDecimalSeparator;
    public string ThousandsSeparator { get; private set; } = DefaultThousandsSeparator;
    public string CurrencyCode { get; private set; } = DefaultCurrencyCode;
    public string PayrollPreviewHelpVisibilityJson { get; private set; } = DefaultPayrollPreviewHelpVisibilityJson;
    public decimal AhvIvEoRate { get; private set; }
    public decimal AlvRate { get; private set; }
    public decimal SicknessAccidentInsuranceRate { get; private set; }
    public decimal TrainingAndHolidayRate { get; private set; }
    public decimal VacationCompensationRate { get; private set; }
    public decimal VacationCompensationRateAge50Plus { get; private set; }
    public decimal VehiclePauschalzone1RateChf { get; private set; }
    public decimal VehiclePauschalzone2RateChf { get; private set; }
    public decimal VehicleRegiezone1RateChf { get; private set; }

    public void UpdateWorkTimeSupplementSettings(WorkTimeSupplementSettings workTimeSupplementSettings)
    {
        ArgumentNullException.ThrowIfNull(workTimeSupplementSettings);

        WorkTimeSupplementSettings = workTimeSupplementSettings;
        Touch();
    }

    public void UpdateCompanyAddress(string? companyAddress)
    {
        CompanyAddress = NormalizeCompanyAddress(companyAddress);
        Touch();
    }

    public void UpdateVisualSettings(
        string? appFontFamily,
        decimal appFontSize,
        string? appTextColorHex,
        string? appMutedTextColorHex,
        string? appBackgroundColorHex,
        string? appAccentColorHex,
        string? appLogoText,
        string? appLogoPath,
        string? printFontFamily,
        decimal printFontSize,
        string? printTextColorHex,
        string? printMutedTextColorHex,
        string? printAccentColorHex,
        string? printLogoText,
        string? printLogoPath)
    {
        AppFontFamily = NormalizeStringOrDefault(appFontFamily, DefaultAppFontFamily);
        AppFontSize = Guard.AgainstNegative(appFontSize, nameof(appFontSize)) == 0m ? DefaultAppFontSize : appFontSize;
        AppTextColorHex = NormalizeStringOrDefault(appTextColorHex, DefaultAppTextColorHex);
        AppMutedTextColorHex = NormalizeStringOrDefault(appMutedTextColorHex, DefaultAppMutedTextColorHex);
        AppBackgroundColorHex = NormalizeStringOrDefault(appBackgroundColorHex, DefaultAppBackgroundColorHex);
        AppAccentColorHex = NormalizeStringOrDefault(appAccentColorHex, DefaultAppAccentColorHex);
        AppLogoText = NormalizeStringOrDefault(appLogoText, DefaultAppLogoText);
        AppLogoPath = NormalizeOptional(appLogoPath) ?? string.Empty;
        PrintFontFamily = NormalizeStringOrDefault(printFontFamily, DefaultPrintFontFamily);
        PrintFontSize = Guard.AgainstNegative(printFontSize, nameof(printFontSize)) == 0m ? DefaultPrintFontSize : printFontSize;
        PrintTextColorHex = NormalizeStringOrDefault(printTextColorHex, DefaultPrintTextColorHex);
        PrintMutedTextColorHex = NormalizeStringOrDefault(printMutedTextColorHex, DefaultPrintMutedTextColorHex);
        PrintAccentColorHex = NormalizeStringOrDefault(printAccentColorHex, DefaultPrintAccentColorHex);
        PrintLogoText = NormalizeStringOrDefault(printLogoText, DefaultPrintLogoText);
        PrintLogoPath = NormalizeOptional(printLogoPath) ?? string.Empty;
        Touch();
    }

    public void UpdateDeductionAndVehicleRates(
        decimal ahvIvEoRate,
        decimal alvRate,
        decimal sicknessAccidentInsuranceRate,
        decimal trainingAndHolidayRate,
        decimal vacationCompensationRate,
        decimal vacationCompensationRateAge50Plus,
        decimal vehiclePauschalzone1RateChf,
        decimal vehiclePauschalzone2RateChf,
        decimal vehicleRegiezone1RateChf)
    {
        AhvIvEoRate = Guard.AgainstNegative(ahvIvEoRate, nameof(ahvIvEoRate));
        AlvRate = Guard.AgainstNegative(alvRate, nameof(alvRate));
        SicknessAccidentInsuranceRate = Guard.AgainstNegative(sicknessAccidentInsuranceRate, nameof(sicknessAccidentInsuranceRate));
        TrainingAndHolidayRate = Guard.AgainstNegative(trainingAndHolidayRate, nameof(trainingAndHolidayRate));
        VacationCompensationRate = Guard.AgainstNegative(vacationCompensationRate, nameof(vacationCompensationRate));
        VacationCompensationRateAge50Plus = Guard.AgainstNegative(vacationCompensationRateAge50Plus, nameof(vacationCompensationRateAge50Plus));
        VehiclePauschalzone1RateChf = Guard.AgainstNegative(vehiclePauschalzone1RateChf, nameof(vehiclePauschalzone1RateChf));
        VehiclePauschalzone2RateChf = Guard.AgainstNegative(vehiclePauschalzone2RateChf, nameof(vehiclePauschalzone2RateChf));
        VehicleRegiezone1RateChf = Guard.AgainstNegative(vehicleRegiezone1RateChf, nameof(vehicleRegiezone1RateChf));
        Touch();
    }

    public decimal GetVacationCompensationRate(DateOnly? birthDate, DateOnly payrollReferenceDate)
    {
        return UsesVacationCompensationRateAge50Plus(birthDate, payrollReferenceDate)
            ? VacationCompensationRateAge50Plus
            : VacationCompensationRate;
    }

    public bool UsesVacationCompensationRateAge50Plus(DateOnly? birthDate, DateOnly payrollReferenceDate)
    {
        var effectiveDate = GetVacationCompensationRateAge50PlusEffectiveDate(birthDate);
        return effectiveDate.HasValue && payrollReferenceDate >= effectiveDate.Value;
    }

    public DateOnly? GetVacationCompensationRateAge50PlusEffectiveDate(DateOnly? birthDate)
    {
        if (!birthDate.HasValue)
        {
            return null;
        }

        return new DateOnly(birthDate.Value.Year + VacationCompensationAge50ThresholdYears, 1, 1);
    }

    public void UpdatePrintTemplate(string? printTemplate)
    {
        PrintTemplate = NormalizeMultilinePreservingSpacing(printTemplate);
        Touch();
    }

    public void UpdateDecimalSeparator(string? decimalSeparator)
    {
        DecimalSeparator = NormalizeDecimalSeparator(decimalSeparator);
        Touch();
    }

    public void UpdateThousandsSeparator(string? thousandsSeparator)
    {
        ThousandsSeparator = NormalizeThousandsSeparator(thousandsSeparator);
        Touch();
    }

    public void UpdateCurrencyCode(string? currencyCode)
    {
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        Touch();
    }

    public void UpdatePayrollPreviewHelpVisibilityJson(string? payrollPreviewHelpVisibilityJson)
    {
        PayrollPreviewHelpVisibilityJson = NormalizeOptional(payrollPreviewHelpVisibilityJson) ?? string.Empty;
        Touch();
    }

    private static string NormalizeCompanyAddress(string? companyAddress)
    {
        if (string.IsNullOrWhiteSpace(companyAddress))
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            companyAddress
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static string NormalizeStringOrDefault(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeMultilinePreservingSpacing(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
    }

    private static string NormalizeDecimalSeparator(string? value)
    {
        return value == "." ? "." : DefaultDecimalSeparator;
    }

    private static string NormalizeThousandsSeparator(string? value)
    {
        return value == " " ? " " : DefaultThousandsSeparator;
    }

    private static string NormalizeCurrencyCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultCurrencyCode
            : value.Trim().ToUpperInvariant();
    }
}
