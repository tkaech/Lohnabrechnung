using Payroll.Application.AnnualSalary;

namespace Payroll.Application.SalaryCertificate;

public sealed class SalaryCertificateService
{
    private readonly AnnualSalaryService _annualSalaryService;

    public SalaryCertificateService(AnnualSalaryService annualSalaryService)
    {
        _annualSalaryService = annualSalaryService;
    }

    public async Task<SalaryCertificateDto> CreateAsync(
        SalaryCertificateQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        _ = new DateOnly(query.Year, 1, 1);

        var annualSalary = await _annualSalaryService.GetOverviewAsync(
            new AnnualSalaryOverviewQuery(query.EmployeeId, query.Year),
            cancellationToken);

        return new SalaryCertificateDto(
            annualSalary.EmployeeId,
            annualSalary.PersonnelNumber,
            annualSalary.FirstName,
            annualSalary.LastName,
            annualSalary.Year,
            CreateFields(annualSalary));
    }

    private static IReadOnlyCollection<SalaryCertificateFieldValueDto> CreateFields(
        AnnualSalaryOverviewDto annualSalary)
    {
        var totals = annualSalary.Totals;

        return
        [
            new(SalaryCertificateFieldCodes.CertificateYear, "Jahr", TextValue: annualSalary.Year.ToString()),
            new(SalaryCertificateFieldCodes.EmployeeAhvNumber, "AHV-Nummer", TextValue: annualSalary.AhvNumber),
            new(SalaryCertificateFieldCodes.EmployeeBirthDate, "Geburtsdatum", DateValue: annualSalary.BirthDate),
            new(SalaryCertificateFieldCodes.SalaryWageCode1, "Lohn", AmountChf: totals.GrossSalaryChf),
            new(SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8, "Bruttolohn total", AmountChf: totals.GrossSalaryChf),
            new(SalaryCertificateFieldCodes.DeductionsSocialSecurityCode9, "AHV/IV/EO/ALV/NBU", AmountChf: totals.SocialInsuranceDeductionChf),
            new(SalaryCertificateFieldCodes.DeductionsPensionFundCode10, "Berufliche Vorsorge", AmountChf: totals.BvgDeductionChf),
            new(SalaryCertificateFieldCodes.SalaryNetWageCode11, "Nettolohn", AmountChf: totals.NetSalaryChf),
            new(SalaryCertificateFieldCodes.TaxSourceTaxCode12, "Quellensteuer", AmountChf: totals.WithholdingTaxChf),
            new(SalaryCertificateFieldCodes.ExpensesCode13, "Spesen", AmountChf: totals.ExpensesChf)
        ];
    }
}
