using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;

namespace HotelBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly BookingService _bookingService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(BookingService bookingService, ILogger<HomeController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(DateOnly? checkIn, DateOnly? checkOut, int? guests)
    {
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

        var model = new HomeIndexViewModel
        {
            Search = searchVm,
            HasSearched = checkIn.HasValue && checkOut.HasValue
        };

        // Server-side date validation
        var error = _bookingService.ValidateStay(cIn, cOut);
        if (error != null)
        {
            model.Search.ErrorMessage = error;
            ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        var availableRooms = await _bookingService.SearchAvailableAsync(cIn, cOut, guestCount);
        var nights = cOut.DayNumber - cIn.DayNumber;

        model.Rooms = availableRooms.Select(r =>
        {
            var pricePerNight = r.PriceOverride ?? r.RoomType.BasePrice;
            return new RoomCardViewModel
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType.Name,
                Description = r.RoomType.Description,
                Capacity = r.RoomType.Capacity,
                PricePerNight = pricePerNight,
                PriceOverride = r.PriceOverride,
                TotalPrice = pricePerNight * nights,
                Nights = nights,
                ImageUrl = r.ImageUrl,
                Amenities = r.RoomAmenities.Select(ra => ra.Amenity.Name).ToList()
            };
        }).ToList();

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
