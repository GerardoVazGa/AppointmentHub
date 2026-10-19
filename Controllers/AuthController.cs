using AppointmentHub.Api.Domain;
using AppointmentHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace AppointmentHub.Api.Controllers;

public record RegisterRequest(string Name, string Email, string Password);

public record UserResponse(int Id, string Name, string Email, UserRole Role);

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase {
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

    public AuthController(AppDbContext db) {
        _db = db;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request) {
        if(String.IsNullOrWhiteSpace(request.Name) || String.IsNullOrWhiteSpace(request.Email)) {
            return BadRequest("Name and Email are required");
        }

        if(request.Password is null || request.Password.Length < 8) {
            return BadRequest("Password must be at least 8 characters long");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _db.Users.AnyAsync(user => user.Email == email);

        if(emailExists) {
            return Conflict("Email is already registered");
        }

        var user = new User {
            Name = request.Name.Trim(),
            Email = email,
            Role = UserRole.Costumer
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.Role));
    }



}