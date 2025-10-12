using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DriveHub.Models;

namespace DriveHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Package> Packages { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; } = null!;
    public DbSet<VehicleBooking> VehicleBookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Fix decimal precision warnings
        builder.Entity<Package>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Entity<Receipt>()
            .Property(r => r.Amount)
            .HasPrecision(18, 2);
    }
}