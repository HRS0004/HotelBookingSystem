namespace HotelBooking.Web.ViewModels;

public class HomeIndexViewModel
{
    public SearchViewModel Search { get; set; } = new();
    public List<RoomCardViewModel> Rooms { get; set; } = new();
    public bool HasSearched { get; set; }
}
