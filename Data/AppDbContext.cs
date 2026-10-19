using AppointmentHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppointmentHub.Api.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Role).HasConversion<string>();
        });
    }
}