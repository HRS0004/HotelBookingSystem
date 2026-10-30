using HotelBooking.Web.Models;

namespace HotelBooking.Web.ViewModels;

public class MyBookingsViewModel
{
    public List<BookingItemViewModel> Bookings { get; set; } = new();
}

public class BookingItemViewModel
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public bool CanCancel { get; set; }
    public DateTime CreatedAt { get; set; }
}
