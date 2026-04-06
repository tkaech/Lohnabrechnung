using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Settings;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Application.Tests;

public sealed class PayrollSettingsRepositorySqliteTests
{
    [Fact]
    public async Task SaveAndLoadAsync_PersistsCentralSupplementRates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PayrollSettingsRepository(dbContext);

        await repository.SaveAsync(new SavePayrollSettingsCommand(0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 1.10m, 2.20m, 3.30m), CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(0.25m, loaded.NightSupplementRate);
        Assert.Equal(0.50m, loaded.SundaySupplementRate);
        Assert.Equal(1.00m, loaded.HolidaySupplementRate);
        Assert.Equal(0.053m, loaded.AhvIvEoRate);
        Assert.Equal(0.011m, loaded.AlvRate);
        Assert.Equal(0.00821m, loaded.SicknessAccidentInsuranceRate);
        Assert.Equal(0.00015m, loaded.TrainingAndHolidayRate);
        Assert.Equal(0.1064m, loaded.VacationCompensationRate);
        Assert.Equal(1.10m, loaded.VehiclePauschalzone1RateChf);
        Assert.Equal(2.20m, loaded.VehiclePauschalzone2RateChf);
        Assert.Equal(3.30m, loaded.VehicleRegiezone1RateChf);
    }
}
