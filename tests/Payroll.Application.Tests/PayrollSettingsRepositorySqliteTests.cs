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

        await repository.SaveAsync(new SavePayrollSettingsCommand(0.25m, 0.50m, 1.00m), CancellationToken.None);
        var loaded = await repository.GetAsync(CancellationToken.None);

        Assert.Equal(0.25m, loaded.NightSupplementRate);
        Assert.Equal(0.50m, loaded.SundaySupplementRate);
        Assert.Equal(1.00m, loaded.HolidaySupplementRate);
    }
}
