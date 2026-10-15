using AppointmentHub.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppointmentHub.Api.Domain;

namespace AppointmentHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class AvailabilityController : ControllerBase {
    private readonly AppDbContext _db;

    public AvailabilityController(AppDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int serviceId, DateOnly date) {
        var service = await _db.Services.FindAsync(serviceId);

        if(service is null) {
            return NotFound("Service not found.");
        }

        var workingHours = await _db.WorkingHours.FirstOrDefaultAsync(wh => wh.DayOfWeek == date.DayOfWeek);

        if(workingHours is null) {
            return Ok(new {
                date,
                isOpen = false
            });
        }

        var dayStart = date.ToDateTime(workingHours.StartTime, DateTimeKind.Utc);
        var dayEnd = date.ToDateTime(workingHours.EndTime, DateTimeKind.Utc);

        var bookings = await _db.Bookings
        .Where(booking => booking.Status == BookingStatus.Confirmed 
            && booking.StartTime < dayEnd && booking.EndTime > dayStart)
        .ToListAsync();

        var duration = TimeSpan.FromMinutes(service.DurationMinutes);

        var slots = new List<DateTime>();

        for(var startAt = dayStart; startAt + duration <= dayEnd; startAt +=duration) {
            var endAt = startAt + duration;
            var taken = bookings.Any(booking => startAt< booking.EndTime && endAt > booking.StartTime);

            if(!taken) {
                slots.Add(startAt);
            }
        }

        return Ok(new {
            date,
            isOpen = true,
            service = service.Name,
            slots
        });

    }
}
