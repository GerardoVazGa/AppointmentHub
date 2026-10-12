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

    [HttpPost]
    public async Task<ActionResult<Service>> Create(Service service) {
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return Ok(service);
    }
}

