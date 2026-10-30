using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Data;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;

namespace HotelBooking.Web.Controllers;

public class RoomsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookingService;

    public RoomsController(ApplicationDbContext db, BookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    [HttpGet]
    public IActionResult Index(DateOnly? checkIn, DateOnly? checkOut, int? guests)
    {
        return RedirectToAction("Index", "Home", new { checkIn, checkOut, guests });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, DateOnly? checkIn, DateOnly? checkOut, int? guests)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (room == null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        var cIn = checkIn ?? today;
        var cOut = checkOut ?? tomorrow;
        var guestCount = (guests.HasValue && guests.Value > 0) ? guests.Value : 2;

        var searchVm = new SearchViewModel
        {
            CheckIn = cIn,
            CheckOut = cOut,
            Guests = guestCount,
            MinCheckInDate = today.ToString("yyyy-MM-dd"),
            MinCheckOutDate = today.AddDays(1).ToString("yyyy-MM-dd")
        };

        var error = _bookingService.ValidateStay(cIn, cOut);
        int? nights = null;
        decimal? totalPrice = null;
        var rate = room.PriceOverride ?? room.RoomType.BasePrice;

        if (error == null)
        {
            nights = cOut.DayNumber - cIn.DayNumber;
            totalPrice = rate * nights.Value;
        }
        else
        {
            searchVm.ErrorMessage = error;
        }

        var model = new RoomDetailsViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomTypeName = room.RoomType.Name,
            Description = room.RoomType.Description,
            Capacity = room.RoomType.Capacity,
            PricePerNight = rate,
            ImageUrl = room.ImageUrl,
            Amenities = room.RoomAmenities.Select(ra => ra.Amenity.Name).ToList(),
            Search = searchVm,
            Nights = nights,
            TotalPrice = totalPrice
        };

        return View(model);
    }
}
