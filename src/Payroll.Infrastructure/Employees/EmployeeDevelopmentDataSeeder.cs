using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Employees;

public static class EmployeeDevelopmentDataSeeder
{
    public static void Seed(PayrollDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (dbContext.Employees.Any())
        {
            return;
        }

        var seeds = CreateSeeds();
        var departments = EnsureDepartmentOptions(dbContext);
        var categories = EnsureEmploymentCategoryOptions(dbContext);
        var locations = EnsureEmploymentLocationOptions(dbContext);

        if (!dbContext.PayrollSettings.Any())
        {
            var settingsSeed = seeds.First();
            dbContext.PayrollSettings.Add(new PayrollSettings(
                workTimeSupplementSettings: new WorkTimeSupplementSettings(
                    settingsSeed.NightSupplementRate,
                    settingsSeed.SundaySupplementRate,
                    settingsSeed.HolidaySupplementRate)));
        }

        foreach (var seed in seeds)
        {
            var employee = new Employee(
                seed.PersonnelNumber,
                seed.FirstName,
                seed.LastName,
                seed.BirthDate,
                seed.EntryDate,
                seed.ExitDate,
                seed.IsActive,
                new EmployeeAddress(
                    seed.Street,
                    seed.HouseNumber,
                    seed.AddressLine2,
                    seed.PostalCode,
                    seed.City,
                    seed.Country),
                seed.ResidenceCountry,
                seed.Nationality,
                seed.PermitCode,
                seed.TaxStatus,
                seed.IsSubjectToWithholdingTax,
                seed.AhvNumber,
                seed.Iban,
                seed.PhoneNumber,
                seed.Email,
                departments[seed.DepartmentName],
                categories[seed.EmploymentCategoryName],
                locations[seed.EmploymentLocationName]);

            var contract = new EmploymentContract(
                employee.Id,
                seed.ContractValidFrom,
                seed.ContractValidTo,
                seed.HourlyRateChf,
                seed.MonthlyBvgDeductionChf,
                3.00m);

            dbContext.Employees.Add(employee);
            dbContext.EmploymentContracts.Add(contract);
        }

        dbContext.SaveChanges();
    }

    internal static IReadOnlyCollection<EmployeeDevelopmentSeed> CreateSeeds()
    {
        return
        [
            new EmployeeDevelopmentSeed("1000", "Nora", "Feld", new DateOnly(1991, 4, 12), new DateOnly(2021, 3, 1), null, true, "Birkenweg", "12", null, "8005", "Zuerich", "Schweiz", "Schweiz", "CH", null, "Ordentlich", false, "756.1000.1000.01", "CH4431999123000889012", "+41 79 101 10 10", "nora.feld@demo-payroll.local", "Sicherheit", "A", "Schachenstr. 7, Emmenbruecke", new DateOnly(2025, 1, 1), null, 33.50m, 310m, 0.25m, 0.50m, 1.00m),
            new EmployeeDevelopmentSeed("1001", "Lio", "Korn", new DateOnly(1988, 9, 3), new DateOnly(2022, 6, 15), null, true, "Aarestrasse", "7a", "Haus B", "3008", "Bern", "Schweiz", "Schweiz", "CH", null, "Ordentlich", false, "756.1000.1000.02", "CH5604835012345678009", "+41 79 101 10 11", "lio.korn@demo-payroll.local", "Buero", "B", "Weinbergstrasse 8, Baar", new DateOnly(2025, 1, 1), null, 31.00m, 280m, 0.25m, null, 1.00m),
            new EmployeeDevelopmentSeed("1002", "Mira", "Tal", new DateOnly(1995, 2, 24), new DateOnly(2024, 1, 1), null, true, "Rue des Acacias", "18", null, "1203", "Geneve", "Schweiz", "Frankreich", "FR", "B", "Quellensteuer B", true, "756.1000.1000.03", "CH9300762011623852957", "+41 79 101 10 12", "mira.tal@demo-payroll.local", "Sicherheit", "C", "Rainstrasse 37, Unteraegeri", new DateOnly(2025, 1, 1), null, 34.00m, 320m, 0.30m, 0.50m, 1.00m),
            new EmployeeDevelopmentSeed("1003", "Jan", "Wald", new DateOnly(1985, 11, 8), new DateOnly(2019, 9, 1), null, true, "Rheinblick", "4", null, "4057", "Basel", "Schweiz", "Deutschland", "DE", "G", "Grenzgaenger", true, "756.1000.1000.04", "CH1200240240240240240", "+41 79 101 10 13", "jan.wald@demo-payroll.local", "Sicherheit", "A", "Schachenstr. 7, Emmenbruecke", new DateOnly(2025, 1, 1), null, 36.50m, 340m, 0.25m, 0.60m, 1.00m),
            new EmployeeDevelopmentSeed("1004", "Tara", "Mond", new DateOnly(1993, 7, 19), new DateOnly(2020, 5, 1), null, true, "Seestrasse", "22", "c/o Atelier Nord", "6005", "Luzern", "Schweiz", "Schweiz", "CH", null, "Ordentlich", false, "756.1000.1000.05", "CH8709000000123456789", "+41 79 101 10 14", "tara.mond@demo-payroll.local", "Buero", "B", "Weinbergstrasse 8, Baar", new DateOnly(2025, 1, 1), null, 32.00m, 295m, null, 0.50m, null),
            new EmployeeDevelopmentSeed("1005", "Enno", "Brink", new DateOnly(1990, 12, 1), new DateOnly(2018, 2, 1), new DateOnly(2025, 12, 31), false, "Gartenhof", "3", null, "8400", "Winterthur", "Schweiz", "Schweiz", "CH", null, "Austretend", false, "756.1000.1000.06", "CH2500235235235235235", "+41 79 101 10 15", "enno.brink@demo-payroll.local", "Sicherheit", "C", "Rainstrasse 37, Unteraegeri", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), 30.50m, 260m, 0.25m, null, null),
            new EmployeeDevelopmentSeed("1006", "Rina", "West", new DateOnly(1997, 1, 30), new DateOnly(2023, 8, 1), null, true, "Via Lago", "11", null, "6900", "Lugano", "Schweiz", "Italien", "IT", "L", "Quellensteuer L", true, "756.1000.1000.07", "CH3908704012345678901", "+41 79 101 10 16", "rina.west@demo-payroll.local", "Buero", "A", "Schachenstr. 7, Emmenbruecke", new DateOnly(2025, 1, 1), null, 35.00m, 300m, 0.25m, 0.50m, 1.00m),
            new EmployeeDevelopmentSeed("1007", "Pio", "Stern", new DateOnly(1982, 6, 11), new DateOnly(2017, 4, 1), new DateOnly(2024, 11, 30), false, "Fabrikstrasse", "27", "3. Stock", "9000", "St. Gallen", "Schweiz", "Schweiz", "CH", null, "Beendet", false, "756.1000.1000.08", "CH1500765432109876543", "+41 79 101 10 17", "pio.stern@demo-payroll.local", "Sicherheit", "B", "Weinbergstrasse 8, Baar", new DateOnly(2024, 1, 1), new DateOnly(2024, 11, 30), 29.50m, 250m, null, null, null),
            new EmployeeDevelopmentSeed("1008", "Yara", "Kiesel", new DateOnly(1999, 3, 17), new DateOnly(2025, 2, 1), null, true, "Muehlegasse", "5", null, "5000", "Aarau", "Schweiz", "Oesterreich", "AT", "B", "Quellensteuer B", true, "756.1000.1000.09", "CH5400765432101234567", "+41 79 101 10 18", "yara.kiesel@demo-payroll.local", "Buero", "C", "Rainstrasse 37, Unteraegeri", new DateOnly(2025, 2, 1), null, 28.75m, 240m, 0.20m, 0.50m, null),
            new EmployeeDevelopmentSeed("1009", "Noel", "Hain", new DateOnly(1987, 10, 25), new DateOnly(2020, 10, 1), null, true, "Bahnhofplatz", "1", "Postfach 9", "7000", "Chur", "Schweiz", "Schweiz", "CH", null, "Ordentlich", false, "756.1000.1000.10", "CH9300900000123456781", "+41 79 101 10 19", "noel.hain@demo-payroll.local", "Sicherheit", "A", "Schachenstr. 7, Emmenbruecke", new DateOnly(2025, 1, 1), null, 37.25m, 355m, 0.25m, 0.50m, 1.00m)
        ];
    }

    private static Dictionary<string, Guid> EnsureDepartmentOptions(PayrollDbContext dbContext)
    {
        return EnsureOptions(dbContext.DepartmentOptions, ["Sicherheit", "Buero"]);
    }

    private static Dictionary<string, Guid> EnsureEmploymentCategoryOptions(PayrollDbContext dbContext)
    {
        return EnsureOptions(dbContext.EmploymentCategoryOptions, ["A", "B", "C"]);
    }

    private static Dictionary<string, Guid> EnsureEmploymentLocationOptions(PayrollDbContext dbContext)
    {
        return EnsureOptions(dbContext.EmploymentLocationOptions,
        [
            "Schachenstr. 7, Emmenbruecke",
            "Weinbergstrasse 8, Baar",
            "Rainstrasse 37, Unteraegeri"
        ]);
    }

    private static Dictionary<string, Guid> EnsureOptions<TOption>(DbSet<TOption> set, IReadOnlyCollection<string> names)
        where TOption : class
    {
        var options = set.ToDictionary(
            option => (string)typeof(TOption).GetProperty("Name")!.GetValue(option)!,
            option => (Guid)typeof(TOption).GetProperty("Id")!.GetValue(option)!,
            StringComparer.OrdinalIgnoreCase);

        foreach (var name in names.Where(name => !options.ContainsKey(name)))
        {
            var option = (TOption)Activator.CreateInstance(typeof(TOption), name)!;
            set.Add(option);
            options[name] = (Guid)typeof(TOption).GetProperty("Id")!.GetValue(option)!;
        }

        return options;
    }
}

internal sealed record EmployeeDevelopmentSeed(
    string PersonnelNumber,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    DateOnly EntryDate,
    DateOnly? ExitDate,
    bool IsActive,
    string Street,
    string? HouseNumber,
    string? AddressLine2,
    string PostalCode,
    string City,
    string Country,
    string? ResidenceCountry,
    string? Nationality,
    string? PermitCode,
    string? TaxStatus,
    bool? IsSubjectToWithholdingTax,
    string? AhvNumber,
    string? Iban,
    string? PhoneNumber,
    string? Email,
    string DepartmentName,
    string EmploymentCategoryName,
    string EmploymentLocationName,
    DateOnly ContractValidFrom,
    DateOnly? ContractValidTo,
    decimal HourlyRateChf,
    decimal MonthlyBvgDeductionChf,
    decimal? NightSupplementRate,
    decimal? SundaySupplementRate,
    decimal? HolidaySupplementRate);
