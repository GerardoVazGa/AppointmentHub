namespace AppointmentHub.Api.Domain;

public class AvailabilityCalculator {
    public static List<DateTime> CalculateAvailableSlots(
        DateTime opensAt,
        DateTime endsAt,
        TimeSpan duration,
        IEnumerable<Booking> bookings
    ) {
        var slots = new List<DateTime>();

        for(var start = opensAt; start + duration <= endsAt; start += duration) {
            var end = start + duration;
            var taken = bookings.Any(booking => start < booking.EndTime && end > booking.StartTime);

            if(!taken) {
                slots.Add(start);
            }
        }

        return slots;
    }
}