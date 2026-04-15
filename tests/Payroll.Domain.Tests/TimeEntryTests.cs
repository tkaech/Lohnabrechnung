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
    public void VehicleCompensationTotal_AggregatesThreeVehicleValues()
    {
        var entry = new TimeEntry(Guid.NewGuid(), new DateOnly(2026, 3, 15), 8m, 0m, 0m, 0m, null, 10m, 20m, 30m);

        Assert.Equal(10m, entry.VehiclePauschalzone1Chf);
        Assert.Equal(20m, entry.VehiclePauschalzone2Chf);
        Assert.Equal(30m, entry.VehicleRegiezone1Chf);
        Assert.Equal(60m, entry.VehicleCompensationTotalChf);
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
