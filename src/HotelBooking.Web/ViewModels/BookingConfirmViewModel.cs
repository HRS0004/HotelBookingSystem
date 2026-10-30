using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.ViewModels;

public class BookingConfirmViewModel
{
    [Required]
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Amenities { get; set; } = new();

    [Required]
    public DateOnly CheckIn { get; set; }

    [Required]
    public DateOnly CheckOut { get; set; }

    public int Nights { get; set; }

    [Range(1, 20)]
    public int Guests { get; set; }

    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }

    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class BookingConfirmInputModel
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateOnly CheckIn { get; set; }

    [Required]
    public DateOnly CheckOut { get; set; }

    [Range(1, 20)]
    public int Guests { get; set; }
}
