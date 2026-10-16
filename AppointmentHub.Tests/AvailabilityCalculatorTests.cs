using AppointmentHub.Api.Domain;

namespace AppointmentHub.Tests;

public class AvailabilityCalculatorTests {
    [Fact]
    public void GetAvailableStartTimes_WithNoBookings_ReturnsAllStartTimes() {
        var opensAt = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
        var closesAt = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(60);
        var bookings = new List<Booking>();

        var result = AvailabilityCalculator.CalculateAvailableSlots(opensAt, closesAt, duration, bookings);

        Assert.Equal(3, result.Count());
        Assert.Equal(opensAt, result[0]);
        Assert.Equal(opensAt.AddHours(2), result[2]);
    }

    [Fact]
    public void GetAvailableStartTimes_WithBookings_ExcludesOverlappingStartTimes() {
        var opensAt = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
        var closesAt = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(60);

        var bookings = new List<Booking>{
            new Booking {
                StartTime = new DateTime(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 10, 20, 11, 0, 0, DateTimeKind.Utc)
            }
        };

        var result = AvailabilityCalculator.CalculateAvailableSlots(opensAt, closesAt, duration, bookings);

        Assert.Equal(2, result.Count);
        Assert.Contains(opensAt, result);
        Assert.Contains(opensAt.AddHours(2), result);
        Assert.DoesNotContain(opensAt.AddHours(1), result);
    }
}
