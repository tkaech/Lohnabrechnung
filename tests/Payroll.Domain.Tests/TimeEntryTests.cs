using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.Tests;

public sealed class TimeEntryTests
{
    [Fact]
    public void SupplementHours_AggregatesNightSundayAndHolidayHours()
    {
        var entry = new TimeEntry(Guid.NewGuid(), new DateOnly(2026, 3, 15), 8.25m, 1.5m, 0.5m, 0.25m);

        Assert.Equal(8.25m, entry.HoursWorked);
        Assert.Equal(1.5m, entry.NightHours);
        Assert.Equal(0.5m, entry.SundayHours);
        Assert.Equal(0.25m, entry.HolidayHours);
        Assert.Equal(2.25m, entry.SupplementHours);
        Assert.Equal(8.25m, entry.TotalHours);
    }

    [Fact]
    public void Constructor_RejectsNegativeSpecialHours()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimeEntry(Guid.NewGuid(), new DateOnly(2026, 3, 15), 8m, -0.25m));
    }

    [Fact]
    public void Constructor_RejectsNightHoursAboveWorkedHours()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimeEntry(Guid.NewGuid(), new DateOnly(2026, 3, 15), 8m, 8.5m));
    }
}
