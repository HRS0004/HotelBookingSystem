using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Areas.Admin.Models;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin")]
[Route("Admin/[controller]")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 1. Today's check-ins: confirmed or checked-in bookings starting today (not cancelled)
        var todaysCheckInsQuery = _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.User)
            .Where(b => b.CheckIn == today && b.Status != BookingStatus.Cancelled);

        var todaysCheckInsCount = await todaysCheckInsQuery.CountAsync();

        // 2. Today's check-outs: bookings departing today (not cancelled)
        var todaysCheckOutsQuery = _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.User)
            .Where(b => b.CheckOut == today && b.Status != BookingStatus.Cancelled);

        var todaysCheckOutsCount = await todaysCheckOutsQuery.CountAsync();

        // 3. Number of occupied rooms today: distinct rooms with CheckedIn status
        var occupiedRoomsCount = await _db.Bookings
            .Where(b => b.Status == BookingStatus.CheckedIn)
            .Select(b => b.RoomId)
            .Distinct()
            .CountAsync();

        // 4. Number of upcoming confirmed bookings: confirmed bookings with check-in >= today
        var upcomingConfirmedBookingsCount = await _db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn >= today)
            .CountAsync();

        // 5. Total rooms: total rooms in system
        var totalRoomsCount = await _db.Rooms.CountAsync();
        var activeRoomsCount = await _db.Rooms.CountAsync(r => r.IsActive);

        var todaysCheckInsList = await todaysCheckInsQuery
            .OrderBy(b => b.Status == BookingStatus.CheckedIn ? 1 : 0)
            .ThenBy(b => b.Room.RoomNumber)
            .Take(10)
            .Select(b => new BookingItemViewModel
            {
                Id = b.Id,
                Reference = b.Reference,
                UserId = b.UserId,
                GuestName = b.User.FullName,
                GuestEmail = b.User.Email ?? string.Empty,
                GuestPhone = b.User.Phone,
                RoomId = b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                RoomTypeName = b.Room.RoomType.Name,
                CheckIn = b.CheckIn,
                CheckOut = b.CheckOut,
                Guests = b.Guests,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                CancelledAt = b.CancelledAt
            })
            .ToListAsync();

        var todaysCheckOutsList = await todaysCheckOutsQuery
            .OrderBy(b => b.Status == BookingStatus.CheckedOut ? 1 : 0)
            .ThenBy(b => b.Room.RoomNumber)
            .Take(10)
            .Select(b => new BookingItemViewModel
            {
                Id = b.Id,
                Reference = b.Reference,
                UserId = b.UserId,
                GuestName = b.User.FullName,
                GuestEmail = b.User.Email ?? string.Empty,
                GuestPhone = b.User.Phone,
                RoomId = b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                RoomTypeName = b.Room.RoomType.Name,
                CheckIn = b.CheckIn,
                CheckOut = b.CheckOut,
                Guests = b.Guests,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                CancelledAt = b.CancelledAt
            })
            .ToListAsync();

        var recentBookings = await _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new BookingItemViewModel
            {
                Id = b.Id,
                Reference = b.Reference,
                UserId = b.UserId,
                GuestName = b.User.FullName,
                GuestEmail = b.User.Email ?? string.Empty,
                GuestPhone = b.User.Phone,
                RoomId = b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                RoomTypeName = b.Room.RoomType.Name,
                CheckIn = b.CheckIn,
                CheckOut = b.CheckOut,
                Guests = b.Guests,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                CancelledAt = b.CancelledAt
            })
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TodaysCheckInsCount = todaysCheckInsCount,
            TodaysCheckOutsCount = todaysCheckOutsCount,
            OccupiedRoomsCount = occupiedRoomsCount,
            UpcomingConfirmedBookingsCount = upcomingConfirmedBookingsCount,
            TotalRoomsCount = totalRoomsCount,
            ActiveRoomsCount = activeRoomsCount,
            TodaysCheckIns = todaysCheckInsList,
            TodaysCheckOuts = todaysCheckOutsList,
            RecentBookings = recentBookings
        };

        return View(vm);
    }
}
