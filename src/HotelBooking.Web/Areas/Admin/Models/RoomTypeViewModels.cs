using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.Areas.Admin.Models;

public class RoomTypeListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public int RoomsCount { get; set; }
}

public class RoomTypeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Room type name is required.")]
    [StringLength(100, ErrorMessage = "Room type name cannot exceed 100 characters.")]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Base price is required.")]
    [Range(0.01, 100000.00, ErrorMessage = "Base price must be greater than 0.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Base Price (per night)")]
    public decimal BasePrice { get; set; }

    [Required(ErrorMessage = "Capacity is required.")]
    [Range(1, 50, ErrorMessage = "Capacity must be between 1 and 50.")]
    [Display(Name = "Guest Capacity")]
    public int Capacity { get; set; } = 2;
}
