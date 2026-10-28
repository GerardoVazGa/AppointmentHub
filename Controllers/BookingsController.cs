using AppointmentHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AppointmentHub.Api.Domain;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentHub.Api.Controllers;

public record CreateBookingsRequest(int ServiceId, DateTime StartTime); 

public record BookingResponse(int Id, string CustomerName, int ServiceId, string? ServiceName, int? UserId, DateTime StartTime, DateTime EndTime, BookingStatus Status);

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase {
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db) {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<BookingResponse>>> GetAll(DateOnly? date) {
        var query = _db.Bookings
            .Include(booking => booking.Service).AsQueryable();

        if(!User.IsInRole(UserRole.Admin.ToString())) {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(!int.TryParse(userIdValue, out var userId)) {
                return Unauthorized();
            }

            query = query.Where(booking => booking.UserId == userId);
        }

        if(date is not null) {
            var startOfDay = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            query = query.Where(booking => booking.StartTime >= startOfDay && booking.EndTime <= endOfDay);
        }

        var bookings = await query.OrderBy(b => b.StartTime).ToListAsync();
        
        return bookings.Select(ToResponse).ToList();
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Booking>> Create(CreateBookingsRequest request) {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!int.TryParse(userIdValue, out var userId)) {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId);

        if(user is null) {
            return Unauthorized();
        }

        var service = await _db.Services.FindAsync(request.ServiceId);

        if(service is null) {
            return NotFound();
        }

        var startTimeNewBooking = request.StartTime;
        var endTimeNewBooking = request.StartTime.AddMinutes(service.DurationMinutes);

        var hasOverlappingBooking = await _db.Bookings.AnyAsync(booking => 
            booking.Status == BookingStatus.Confirmed &&
            startTimeNewBooking < booking.EndTime && endTimeNewBooking > booking.StartTime

        );

        if(hasOverlappingBooking) {
            return Conflict("That time slot is already booked.");
        }

        var booking = new Booking {
            CustomerName = user.Name,
            UserId = user.Id,
            ServiceId = service.Id,
            StartTime = startTimeNewBooking,
            EndTime = endTimeNewBooking
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(booking));
    }

    private static BookingResponse ToResponse(Booking booking) {
        return new BookingResponse (
            booking.Id,
            booking.CustomerName,
            booking.ServiceId,
            booking.Service?.Name,
            booking.UserId,
            booking.StartTime,
            booking.EndTime,
            booking.Status
        );
    }

    [Authorize]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id) {
        var booking = await _db.Bookings.FindAsync(id);

        if(booking is null) {
            return NotFound();
        }

        if(!User.IsInRole(UserRole.Admin.ToString())) {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(!int.TryParse(userIdValue, out var userId) || booking.UserId != userId) {
                return Forbid();
            }
        }

        if(booking.Status == BookingStatus.Cancelled) {
            return Conflict("Booking is already cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;

        await _db.SaveChangesAsync();

        return NoContent();
    }

}




