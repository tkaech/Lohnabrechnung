using Microsoft.EntityFrameworkCore;
using Payroll.Application.SalaryCertificate;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.SalaryCertificate;

public sealed class SalaryCertificateRecordRepository : ISalaryCertificateRecordRepository
{
    private readonly PayrollDbContext _dbContext;

    public SalaryCertificateRecordRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord record)
    {
        _dbContext.SalaryCertificateRecords.Add(record);
    }

    public async Task<SalaryCertificateRecordDto?> GetLatestAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SalaryCertificateRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.Year == year)
            .ToListAsync(cancellationToken);

        return records
            .OrderByDescending(record => record.CreatedAtUtc)
            .Select(record => new SalaryCertificateRecordDto(
                record.Id,
                record.EmployeeId,
                record.Year,
                record.CreatedAtUtc,
                record.OutputFilePath,
                record.FileHash))
            .FirstOrDefault();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
