namespace HotelBooking.Web.Areas.Admin.Models;

public class DashboardViewModel
{
    public int TodaysCheckInsCount { get; set; }
    public int TodaysCheckOutsCount { get; set; }
    public int OccupiedRoomsCount { get; set; }
    public int UpcomingConfirmedBookingsCount { get; set; }
    public int TotalRoomsCount { get; set; }
    public int ActiveRoomsCount { get; set; }

    public List<BookingItemViewModel> TodaysCheckIns { get; set; } = new();
    public List<BookingItemViewModel> TodaysCheckOuts { get; set; } = new();
    public List<BookingItemViewModel> RecentBookings { get; set; } = new();
}
