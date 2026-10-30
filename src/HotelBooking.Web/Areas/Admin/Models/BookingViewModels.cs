using System.ComponentModel.DataAnnotations;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Areas.Admin.Models;

public class BookingItemViewModel
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public bool CanCheckIn => Status == BookingStatus.Confirmed;
    public bool CanCheckOut => Status == BookingStatus.CheckedIn;
    public bool CanCancel => Status == BookingStatus.Confirmed;
}

public class BookingListViewModel
{
    public BookingStatus? StatusFilter { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Check-in From")]
    public DateOnly? FromDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Check-in To")]
    public DateOnly? ToDate { get; set; }

    [Display(Name = "Search Reference or Guest Email")]
    public string? Search { get; set; }

    public List<BookingItemViewModel> Bookings { get; set; } = new();

    public int TotalCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CheckedInCount { get; set; }
    public int CheckedOutCount { get; set; }
    public int CancelledCount { get; set; }
}
