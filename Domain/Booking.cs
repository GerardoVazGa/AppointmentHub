namespace AppointmentHub.Api.Domain;

public class Booking {
    public int Id { get; set; }
    public string CustomerName { get; set; } = " ";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
}