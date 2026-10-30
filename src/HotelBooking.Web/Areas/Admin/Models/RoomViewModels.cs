using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelBooking.Web.Areas.Admin.Models;

public class RoomListItemViewModel
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Floor { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal EffectivePrice => PriceOverride ?? BasePrice;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public List<string> AmenityNames { get; set; } = new();
    public int FutureConfirmedBookingsCount { get; set; }
}

public class AmenityCheckboxItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class RoomFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Room number is required.")]
    [StringLength(20, ErrorMessage = "Room number cannot exceed 20 characters.")]
    [Display(Name = "Room Number")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a room type.")]
    [Display(Name = "Room Type")]
    public int RoomTypeId { get; set; }

    [Required(ErrorMessage = "Floor is required.")]
    [Range(0, 100, ErrorMessage = "Floor must be between 0 and 100.")]
    [Display(Name = "Floor")]
    public int Floor { get; set; } = 1;

    [Range(0.01, 100000.00, ErrorMessage = "Price override must be greater than 0.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price Override (Optional, per night)")]
    public decimal? PriceOverride { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL.")]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Amenities")]
    public List<int> SelectedAmenityIds { get; set; } = new();

    public SelectList? RoomTypeOptions { get; set; }
    public List<AmenityCheckboxItem> AvailableAmenities { get; set; } = new();

    public bool HasFutureConfirmedBookings { get; set; }
    public int FutureConfirmedBookingsCount { get; set; }
}
