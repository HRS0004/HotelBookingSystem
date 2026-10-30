using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Services;

public record BookingResult(bool Success, string? Error = null, Booking? Booking = null);

public class BookingService
{
    private const int MaxNights = 14;
    private readonly ApplicationDbContext _db;

    public BookingService(ApplicationDbContext db) => _db = db;

    public string? ValidateStay(DateOnly checkIn, DateOnly checkOut)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (checkIn < today) return "Check-in cannot be in the past.";
        if (checkOut <= checkIn) return "Check-out must be after check-in.";
        if (checkOut.DayNumber - checkIn.DayNumber > MaxNights) return $"Maximum stay is {MaxNights} nights.";
        if (checkIn > today.AddYears(1)) return "Bookings open up to one year ahead.";
        return null;
    }

    public async Task<List<Room>> SearchAvailableAsync(DateOnly checkIn, DateOnly checkOut, int guests)
    {
        return await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .Where(r => r.IsActive && r.RoomType.Capacity >= guests)
            .Where(r => !r.Bookings.Any(b => b.Status != BookingStatus.Cancelled
                                          && b.CheckIn < checkOut && b.CheckOut > checkIn))
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<BookingResult> CreateAsync(string userId, int roomId, DateOnly checkIn, DateOnly checkOut, int guests)
    {
        var error = ValidateStay(checkIn, checkOut);
        if (error != null) return new(false, error);

        await using var tx = await _db.Database.BeginTransactionAsync();

        // No-op update that takes a lock on this room's row. Concurrent requests for the
        // same room queue here. The database runs with READ_COMMITTED_SNAPSHOT on, so plain
        // reads would not block each other and two guests could pass the check together.
        await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Rooms SET IsActive = IsActive WHERE Id = {roomId}");

        var room = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null || !room.IsActive) return new(false, "This room is not available.");
        if (guests < 1 || guests > room.RoomType.Capacity)
            return new(false, $"This room sleeps {room.RoomType.Capacity} guests at most.");

        var overlaps = await _db.Bookings.AnyAsync(b => b.RoomId == roomId
            && b.Status != BookingStatus.Cancelled
            && b.CheckIn < checkOut && b.CheckOut > checkIn);
        if (overlaps) return new(false, "Sorry, those dates were just taken.");

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var rate = room.PriceOverride ?? room.RoomType.BasePrice;

        var booking = new Booking
        {
            Reference = "PS-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            UserId = userId,
            RoomId = roomId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = guests,
            TotalPrice = rate * nights, // saved now so later price changes don't alter old bookings
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, null, booking);
    }

    public async Task<BookingResult> CancelAsync(int bookingId, string userId, bool isAdmin)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null || (!isAdmin && booking.UserId != userId))
            return new(false, "Booking not found.");
        if (booking.Status != BookingStatus.Confirmed)
            return new(false, "Only confirmed bookings can be cancelled.");

        // Assumes a 2 PM check-in time. Guests cancel free until 24 hours before it.
        var cutoff = booking.CheckIn.ToDateTime(new TimeOnly(14, 0)).AddHours(-24);
        if (!isAdmin && DateTime.Now > cutoff)
            return new(false, "Free cancellation ended 24 hours before check-in.");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, null, booking);
    }
}