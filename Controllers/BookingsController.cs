using AppointmentHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AppointmentHub.Api.Domain;

namespace AppointmentHub.Api.Controllers;

public record CreateBookingsRequest(string CustomerName, int ServiceId, DateTime StartTime); 

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase {
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Booking>>> GetAll(DateOnly? date) {
        var query = _db.Bookings
            .Include(booking => booking.Service).AsQueryable();

        if(date is not null) {
            var startOfDay = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            query = query.Where(booking => booking.StartTime >= startOfDay && booking.EndTime <= endOfDay);
        }
        
        return await query.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> Create(CreateBookingsRequest request) {
        var service = await _db.Services.FindAsync(request.ServiceId);

        if(service is null) {
            return NotFound();
        }

        var startTimeNewBooking = request.StartTime;
        var endTimeNewBooking = request.StartTime.AddMinutes(service.DurationMinutes);

        var hasOverlappingBooking = await _db.Bookings.AnyAsync(booking => 
            startTimeNewBooking < booking.EndTime && endTimeNewBooking > booking.StartTime

        );

        if(hasOverlappingBooking) {
            return Conflict("That time slot is already booked.");
        }

        var booking = new Booking {
            CustomerName = request.CustomerName,
            ServiceId = service.Id,
            StartTime = startTimeNewBooking,
            EndTime = endTimeNewBooking
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        return Ok(booking);
    }
}




