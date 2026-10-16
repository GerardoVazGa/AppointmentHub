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
}
