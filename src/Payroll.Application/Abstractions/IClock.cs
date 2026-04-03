namespace Payroll.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
