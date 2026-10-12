using AppointmentHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppointmentHub.Api.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
}