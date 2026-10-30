namespace HotelBooking.Web.ViewModels;

public class RoomCardViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal TotalPrice { get; set; }
    public int Nights { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Amenities { get; set; } = new();
}
