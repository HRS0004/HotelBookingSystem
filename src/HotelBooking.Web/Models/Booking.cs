namespace HotelBooking.Web.Models;

public class Booking
{
    public int Id { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public int Guests { get; set; }

    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}

