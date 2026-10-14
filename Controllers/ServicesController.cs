using AppointmentHub.Api.Data;
using AppointmentHub.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentHub.Api.Controllers;

public record CreateServiceRequest(string Name, int DurationMinutes, decimal Price);

[ApiController]
[Route("api/[controller]")]

public class ServicesController : ControllerBase {
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Service>>> GetAll() {
        var services = await _db.Services.ToListAsync();
        return services;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> GetById(int id) {
        var service = await _db.Services.FindAsync(id);

        if(service is null) {
            return NotFound();
        }

        return service;
    }

    [HttpPost]
    public async Task<ActionResult<Service>> Create(CreateServiceRequest request) {
        if(string.IsNullOrWhiteSpace(request.Name)) {
            return BadRequest("Service name cannot be empty.");
            
        }

        var service = new Service {
            Name = request.Name,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price
        };
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return Ok(service);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Service>> Update(int id, CreateServiceRequest updated) {

        if(string.IsNullOrWhiteSpace(updated.Name)) {
            return BadRequest("Service name cannot be empty.");
        }

        if(updated.DurationMinutes < 0) {
            return BadRequest("Duration must be greater than 0.");
        }

        if(updated.Price < 0) {
            return BadRequest("Price must be greater than 0.");
        }

        var service = await _db.Services.FindAsync(id);

        if(service is null) {
            return NotFound();
        }

        service.Name = updated.Name;
        service.DurationMinutes = updated.DurationMinutes;
        service.Price = updated.Price;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id) {
        var service = await _db.Services.FindAsync(id);

        if(service is null) {
            return NotFound();
        }

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

