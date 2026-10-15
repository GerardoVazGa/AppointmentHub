using AppointmentHub.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        return Ok(new {
            date,
            isOpen = true,
            service = service.Name,
            opensAt = workingHours.StartTime,
            closesAt = workingHours.EndTime
        });

    }
}
