using AppointmentHub.Api.Data;
using AppointmentHub.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentHub.Api.Controllers;

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
    public async Task<ActionResult<Service>> Create(Service service) {
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return Ok(service);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Service>> Update(int id, Service updated) {
        var service = await _db.Services.FindAsync(id);

        if(service is null) {
            return NotFound();
        }

        service.Name = updated.Name;
        service.DurationMinutes = updated.DurationMinutes;
        service.price = updated.price;

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

