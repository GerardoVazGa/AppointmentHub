using AppointmentHub.Api.Domain;
using AppointmentHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentHub.Api.Controllers;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);

public record UserResponse(int Id, string Name, string Email, UserRole Role);

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase {
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

    public AuthController(AppDbContext db, IConfiguration config) {
        _db = db;
        _config = config;
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) {
        if(String.IsNullOrWhiteSpace(request.Email) || String.IsNullOrWhiteSpace(request.Password)) {
            return BadRequest("Email and Password are required");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(user => user.Email == email);

        if(user is null) {
            return Unauthorized("Invalid email or password");
        }

        var passwordCorrect = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if(passwordCorrect == PasswordVerificationResult.Failed) {
            return Unauthorized("Invalid email or password");
        }

        var token = GenerateToken(user);

        return Ok(new LoginResponse(token));

    }

    private string GenerateToken(User user) {
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


}