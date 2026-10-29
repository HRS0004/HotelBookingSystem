namespace HotelBooking.Web.Models;

public class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public int RoomTypeId { get; set; }

    public RoomType RoomType { get; set; } = null!;

    public int Floor { get; set; }

    public decimal? PriceOverride { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

