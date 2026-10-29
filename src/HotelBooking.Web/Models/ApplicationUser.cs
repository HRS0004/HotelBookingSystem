using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

