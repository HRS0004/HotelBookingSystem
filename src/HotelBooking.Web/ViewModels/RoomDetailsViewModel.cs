namespace HotelBooking.Web.ViewModels;

public class RoomDetailsViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Amenities { get; set; } = new();
    public SearchViewModel Search { get; set; } = new();
    public int? Nights { get; set; }
    public decimal? TotalPrice { get; set; }
}
