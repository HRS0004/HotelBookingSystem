using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RoomType> RoomTypes { get; set; } = default!;
    public DbSet<Room> Rooms { get; set; } = default!;
    public DbSet<Amenity> Amenities { get; set; } = default!;
    public DbSet<RoomAmenity> RoomAmenities { get; set; } = default!;
    public DbSet<Booking> Bookings { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RoomType>(entity =>
        {
            entity.Property(rt => rt.BasePrice)
                .HasPrecision(10, 2);
        });

        builder.Entity<Room>(entity =>
        {
            entity.HasIndex(r => r.RoomNumber)
                .IsUnique();

            entity.Property(r => r.PriceOverride)
                .HasPrecision(10, 2);

            entity.Property(r => r.IsActive)
                .HasDefaultValue(true);

            entity.HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Amenity>(entity =>
        {
            entity.HasIndex(a => a.Name)
                .IsUnique();
        });

        builder.Entity<RoomAmenity>(entity =>
        {
            entity.HasKey(ra => new { ra.RoomId, ra.AmenityId });

            entity.HasOne(ra => ra.Room)
                .WithMany(r => r.RoomAmenities)
                .HasForeignKey(ra => ra.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ra => ra.Amenity)
                .WithMany(a => a.RoomAmenities)
                .HasForeignKey(ra => ra.AmenityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.Reference)
                .IsUnique();

            entity.HasIndex(b => new { b.RoomId, b.CheckIn, b.CheckOut });

            entity.Property(b => b.TotalPrice)
                .HasPrecision(10, 2);

            entity.Property(b => b.Status)
                .HasConversion<string>();

            entity.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Booking_CheckOut_CheckIn", "CheckOut > CheckIn");
                t.HasCheckConstraint("CK_Booking_TotalPrice", "TotalPrice > 0");
                t.HasCheckConstraint("CK_Booking_Guests", "Guests > 0");
            });
        });
    }
}
