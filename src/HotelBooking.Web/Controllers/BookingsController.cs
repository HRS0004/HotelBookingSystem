using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;

namespace HotelBooking.Web.Controllers;

[Authorize]
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var bookings = await _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var model = new MyBookingsViewModel
        {
            Bookings = bookings.Select(b =>
            {
                // Assumes a 2 PM check-in time. Free cancellation ends 24 hours prior.
                var cutoff = b.CheckIn.ToDateTime(new TimeOnly(14, 0)).AddHours(-24);
                var canCancel = b.Status == BookingStatus.Confirmed && DateTime.Now <= cutoff;
                var nights = b.CheckOut.DayNumber - b.CheckIn.DayNumber;

                return new BookingItemViewModel
                {
                    Id = b.Id,
                    Reference = b.Reference,
                    RoomNumber = b.Room.RoomNumber,
                    RoomTypeName = b.Room.RoomType.Name,
                    ImageUrl = b.Room.ImageUrl,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    Nights = nights,
                    Guests = b.Guests,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    CanCancel = canCancel,
                    CreatedAt = b.CreatedAt
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int roomId, DateOnly checkIn, DateOnly checkOut, int guests = 1)
    {
        var stayError = _bookingService.ValidateStay(checkIn, checkOut);
        if (stayError != null)
        {
            TempData["ErrorMessage"] = stayError;
            return RedirectToAction("Details", "Rooms", new { id = roomId, checkIn, checkOut, guests });
        }

        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);

        if (room == null)
        {
            return NotFound();
        }

        if (guests < 1 || guests > room.RoomType.Capacity)
        {
            TempData["ErrorMessage"] = $"This room accommodates at most {room.RoomType.Capacity} guests.";
            return RedirectToAction("Details", "Rooms", new { id = roomId, checkIn, checkOut, guests });
        }

        var user = await _userManager.GetUserAsync(User);
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var rate = room.PriceOverride ?? room.RoomType.BasePrice;
        var totalPrice = rate * nights;

        var model = new BookingConfirmViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeName = room.RoomType.Name,
            Floor = room.Floor,
            ImageUrl = room.ImageUrl,
            Amenities = room.RoomAmenities.Select(ra => ra.Amenity.Name).ToList(),
            CheckIn = checkIn,
            CheckOut = checkOut,
            Nights = nights,
            Guests = guests,
            PricePerNight = rate,
            TotalPrice = totalPrice,
            GuestName = user?.FullName ?? user?.UserName ?? "Guest",
            GuestEmail = user?.Email ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(BookingConfirmInputModel input)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var result = await _bookingService.CreateAsync(userId, input.RoomId, input.CheckIn, input.CheckOut, input.Guests);

        if (!result.Success)
        {
            // Reload room and user details to display the confirmation form again with the service error
            var room = await _db.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .FirstOrDefaultAsync(r => r.Id == input.RoomId);

            var user = await _userManager.GetUserAsync(User);
            var nights = input.CheckOut > input.CheckIn ? input.CheckOut.DayNumber - input.CheckIn.DayNumber : 1;
            var rate = room != null ? (room.PriceOverride ?? room.RoomType.BasePrice) : 0m;

            var viewModel = new BookingConfirmViewModel
            {
                RoomId = input.RoomId,
                RoomNumber = room?.RoomNumber ?? string.Empty,
                RoomTypeName = room?.RoomType.Name ?? string.Empty,
                Floor = room?.Floor ?? 1,
                ImageUrl = room?.ImageUrl,
                Amenities = room?.RoomAmenities.Select(ra => ra.Amenity.Name).ToList() ?? new(),
                CheckIn = input.CheckIn,
                CheckOut = input.CheckOut,
                Nights = nights,
                Guests = input.Guests,
                PricePerNight = rate,
                TotalPrice = rate * nights,
                GuestName = user?.FullName ?? user?.UserName ?? "Guest",
                GuestEmail = user?.Email ?? "",
                ErrorMessage = result.Error
            };

            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to complete booking.");
            return View(viewModel);
        }

        return RedirectToAction(nameof(Confirmation), new { reference = result.Booking!.Reference });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(string reference)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var booking = await _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(b => b.Reference == reference && b.UserId == userId);

        if (booking == null)
        {
            return NotFound();
        }

        var nights = booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;

        var model = new BookingConfirmationViewModel
        {
            Reference = booking.Reference,
            RoomNumber = booking.Room.RoomNumber,
            RoomTypeName = booking.Room.RoomType.Name,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Nights = nights,
            Guests = booking.Guests,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var result = await _bookingService.CancelAsync(id, userId, isAdmin: false);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = $"Booking {result.Booking?.Reference} was cancelled successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
