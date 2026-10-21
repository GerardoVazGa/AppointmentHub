using AppointmentHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AppointmentHub.Api.Domain;
using Microsoft.AspNetCore.Authorization;

namespace AppointmentHub.Api.Controllers;

public record WorkingHoursRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/working-hours")]
public class WorkingHoursController : ControllerBase {
    private readonly AppDbContext _db;

    public WorkingHoursController(AppDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkingHours>>> GetAll() {
        var workingHours = await _db.WorkingHours.ToListAsync();

        return workingHours;
    }

    [HttpPost]
    public async Task<ActionResult<WorkingHours>> Create(WorkingHoursRequest request) {

        if(request.StartTime >= request.EndTime) {
            return BadRequest("Start time must be before end time.");
        }

        if(!Enum.IsDefined(typeof(DayOfWeek), request.DayOfWeek)) {
            return BadRequest("Invalid day of week.");
        }

        var exists = await _db.WorkingHours.AnyAsync(wh => wh.DayOfWeek == request.DayOfWeek);

        if(exists) {
            return Conflict("Working hours for this day already exist.");
        }

        var workingHours = new WorkingHours {
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };
        

        _db.WorkingHours.Add(workingHours);
        await _db.SaveChangesAsync();
        return Ok(workingHours);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, WorkingHoursRequest updated) {

        if(updated.StartTime >= updated.EndTime) {
            return BadRequest("Start time must be before end time.");
        }

        if(!Enum.IsDefined(typeof(DayOfWeek), updated.DayOfWeek)) {
            return BadRequest("Invalid day of week.");
        }

        var workingHours = await _db.WorkingHours.FindAsync(id);

        if(workingHours is null) {
            return NotFound();
        }
        
        var exists = await _db.WorkingHours.AnyAsync(wh => wh.DayOfWeek == updated.DayOfWeek && wh.Id != id);

        if(exists) {
            return Conflict("Working hours for this day already exist.");
        }


        workingHours.DayOfWeek = updated.DayOfWeek;
        workingHours.StartTime = updated.StartTime;
        workingHours.EndTime = updated.EndTime;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) {
        var workingHours = await _db.WorkingHours.FindAsync(id);

        if(workingHours is null) {
            return NotFound();
        }

        _db.WorkingHours.Remove(workingHours);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    
}