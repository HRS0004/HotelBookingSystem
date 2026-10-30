using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Areas.Admin.Models;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;

namespace HotelBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingsController(
        ApplicationDbContext db,
        BookingService bookingService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _bookingService = bookingService;
        _userManager = userManager;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        [FromQuery] BookingStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search)
    {
        // Status counts across all bookings for the quick tabs/badges
        var totalCount = await _db.Bookings.CountAsync();
        var confirmedCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed);
        var checkedInCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.CheckedIn);
        var checkedOutCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.CheckedOut);
        var cancelledCount = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled);

        var query = _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.User)
            .AsQueryable();

        // 1. Filter by Status
        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        // 2. Filter by Date range (Check-in from / to)
        if (fromDate.HasValue)
        {
            query = query.Where(b => b.CheckIn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.CheckIn <= toDate.Value);
        }

        // 3. Search by Reference or Guest Email (and Name)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(b =>
                b.Reference.Contains(s) ||
                (b.User.Email != null && b.User.Email.Contains(s)) ||
                (b.User.FullName != null && b.User.FullName.Contains(s)));
        }

        var items = await query
            .OrderByDescending(b => b.CheckIn)
            .ThenByDescending(b => b.CreatedAt)
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

        var model = new BookingListViewModel
        {
            StatusFilter = status,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search,
            Bookings = items,
            TotalCount = totalCount,
            ConfirmedCount = confirmedCount,
            CheckedInCount = checkedInCount,
            CheckedOutCount = checkedOutCount,
            CancelledCount = cancelledCount
        };

        return View(model);
    }

    [HttpPost("CheckIn/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(
        int id,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        // Enforce valid status transition: Only Confirmed -> CheckedIn
        if (booking.Status != BookingStatus.Confirmed)
        {
            TempData["Error"] = $"Cannot check in booking {booking.Reference}. Only Confirmed bookings can be marked Checked In (current status: {booking.Status}).";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        booking.Status = BookingStatus.CheckedIn;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Booking {booking.Reference} (Room {booking.Room.RoomNumber}) is now Checked In.";
        return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
    }

    [HttpPost("CheckOut/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(
        int id,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        // Enforce valid status transition: Only CheckedIn -> CheckedOut
        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = $"Cannot check out booking {booking.Reference}. Only Checked In bookings can be marked Checked Out (current status: {booking.Status}).";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        booking.Status = BookingStatus.CheckedOut;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Booking {booking.Reference} (Room {booking.Room.RoomNumber}) has been Checked Out.";
        return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
    }

    [HttpPost("Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        // Enforce valid status transition: Only Confirmed -> Cancelled
        if (booking.Status != BookingStatus.Confirmed)
        {
            TempData["Error"] = $"Cannot cancel booking {booking.Reference}. Only Confirmed bookings can be cancelled (current status: {booking.Status}).";
            return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
        }

        var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
        var result = await _bookingService.CancelAsync(id, currentUserId, isAdmin: true);

        if (result.Success)
        {
            TempData["Success"] = $"Booking {booking.Reference} has been cancelled successfully.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to cancel booking.";
        }

        return RedirectToAction(nameof(Index), new { status, fromDate, toDate, search });
    }
}
