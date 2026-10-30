namespace HotelBooking.Web.ViewModels;

public class SearchViewModel
{
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Guests { get; set; } = 2;
    public string MinCheckInDate { get; set; } = string.Empty;
    public string MinCheckOutDate { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
