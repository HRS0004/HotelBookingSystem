using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Areas.Admin.Models;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
public class RoomsController : Controller
{
    private readonly ApplicationDbContext _db;

    public RoomsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
            .Include(r => r.Bookings)
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new RoomListItemViewModel
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                Capacity = r.RoomType.Capacity,
                Floor = r.Floor,
                BasePrice = r.RoomType.BasePrice,
                PriceOverride = r.PriceOverride,
                ImageUrl = r.ImageUrl,
                IsActive = r.IsActive,
                AmenityNames = r.RoomAmenities.Select(ra => ra.Amenity.Name).OrderBy(n => n).ToList(),
                FutureConfirmedBookingsCount = r.Bookings.Count(b => b.Status == BookingStatus.Confirmed && b.CheckOut >= today)
            })
            .ToListAsync();

        return View(rooms);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = new RoomFormViewModel
        {
            IsActive = true,
            Floor = 1
        };

        await PopulateFormDataAsync(model);
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomFormViewModel model)
    {
        var roomNumberTrimmed = model.RoomNumber?.Trim() ?? string.Empty;

        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == roomNumberTrimmed))
        {
            ModelState.AddModelError(nameof(model.RoomNumber), $"Room number '{roomNumberTrimmed}' already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormDataAsync(model);
            return View(model);
        }

        var room = new Room
        {
            RoomNumber = roomNumberTrimmed,
            RoomTypeId = model.RoomTypeId,
            Floor = model.Floor,
            PriceOverride = model.PriceOverride,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
            IsActive = model.IsActive
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        if (model.SelectedAmenityIds.Any())
        {
            var validAmenityIds = await _db.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var amenityId in validAmenityIds)
            {
                _db.RoomAmenities.Add(new RoomAmenity
                {
                    RoomId = room.Id,
                    AmenityId = amenityId
                });
            }

            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"Room {room.RoomNumber} was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomAmenities)
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureBookingsCount = room.Bookings.Count(b => b.Status == BookingStatus.Confirmed && b.CheckOut >= today);

        var model = new RoomFormViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeId = room.RoomTypeId,
            Floor = room.Floor,
            PriceOverride = room.PriceOverride,
            ImageUrl = room.ImageUrl,
            IsActive = room.IsActive,
            SelectedAmenityIds = room.RoomAmenities.Select(ra => ra.AmenityId).ToList(),
            HasFutureConfirmedBookings = futureBookingsCount > 0,
            FutureConfirmedBookingsCount = futureBookingsCount
        };

        await PopulateFormDataAsync(model);
        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var room = await _db.Rooms
            .Include(r => r.RoomAmenities)
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        var roomNumberTrimmed = model.RoomNumber?.Trim() ?? string.Empty;

        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == roomNumberTrimmed && r.Id != id))
        {
            ModelState.AddModelError(nameof(model.RoomNumber), $"Room number '{roomNumberTrimmed}' already exists.");
        }

        if (!ModelState.IsValid)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            model.FutureConfirmedBookingsCount = room.Bookings.Count(b => b.Status == BookingStatus.Confirmed && b.CheckOut >= today);
            model.HasFutureConfirmedBookings = model.FutureConfirmedBookingsCount > 0;
            await PopulateFormDataAsync(model);
            return View(model);
        }

        room.RoomNumber = roomNumberTrimmed;
        room.RoomTypeId = model.RoomTypeId;
        room.Floor = model.Floor;
        room.PriceOverride = model.PriceOverride;
        room.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        room.IsActive = model.IsActive;

        // Update amenities
        var currentAmenityIds = room.RoomAmenities.Select(ra => ra.AmenityId).ToHashSet();
        var selectedAmenityIds = model.SelectedAmenityIds.ToHashSet();

        // Remove unselected
        var toRemove = room.RoomAmenities.Where(ra => !selectedAmenityIds.Contains(ra.AmenityId)).ToList();
        foreach (var ra in toRemove)
        {
            _db.RoomAmenities.Remove(ra);
        }

        // Add new
        var toAdd = selectedAmenityIds.Where(idToAdd => !currentAmenityIds.Contains(idToAdd)).ToList();
        var validNewAmenityIds = await _db.Amenities
            .Where(a => toAdd.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var amenityId in validNewAmenityIds)
        {
            _db.RoomAmenities.Add(new RoomAmenity
            {
                RoomId = room.Id,
                AmenityId = amenityId
            });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room {room.RoomNumber} was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureConfirmedCount = room.Bookings.Count(b => b.Status == BookingStatus.Confirmed && b.CheckOut >= today);

        var model = new RoomListItemViewModel
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.RoomType.Name,
            Capacity = room.RoomType.Capacity,
            Floor = room.Floor,
            BasePrice = room.RoomType.BasePrice,
            PriceOverride = room.PriceOverride,
            ImageUrl = room.ImageUrl,
            IsActive = room.IsActive,
            FutureConfirmedBookingsCount = futureConfirmedCount
        };

        return View(model);
    }

    [HttpPost("Delete/{id}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasFutureConfirmed = await _db.Bookings.AnyAsync(b =>
            b.RoomId == id &&
            b.Status == BookingStatus.Confirmed &&
            b.CheckOut >= today);

        if (hasFutureConfirmed)
        {
            TempData["Error"] = $"Cannot delete Room {room.RoomNumber} because it has future confirmed booking(s). You can deactivate it instead.";
            TempData["OfferDeactivateRoomId"] = room.Id;
            TempData["OfferDeactivateRoomNumber"] = room.RoomNumber;
            return RedirectToAction(nameof(Index));
        }

        var hasAnyBookings = await _db.Bookings.AnyAsync(b => b.RoomId == id);
        if (hasAnyBookings)
        {
            TempData["Error"] = $"Cannot delete Room {room.RoomNumber} because historical booking records reference it. You can deactivate it instead.";
            TempData["OfferDeactivateRoomId"] = room.Id;
            TempData["OfferDeactivateRoomNumber"] = room.RoomNumber;
            return RedirectToAction(nameof(Index));
        }

        var amenities = await _db.RoomAmenities.Where(ra => ra.RoomId == id).ToListAsync();
        _db.RoomAmenities.RemoveRange(amenities);
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room {room.RoomNumber} was successfully deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        room.IsActive = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room {room.RoomNumber} has been deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
        {
            TempData["Error"] = "Room not found.";
            return RedirectToAction(nameof(Index));
        }

        room.IsActive = !room.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room {room.RoomNumber} is now {(room.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFormDataAsync(RoomFormViewModel model)
    {
        var roomTypes = await _db.RoomTypes
            .OrderBy(rt => rt.Name)
            .Select(rt => new { rt.Id, Display = $"{rt.Name} (Capacity: {rt.Capacity}, Base: ₹{rt.BasePrice:N0})" })
            .ToListAsync();

        model.RoomTypeOptions = new SelectList(roomTypes, "Id", "Display", model.RoomTypeId);

        var allAmenities = await _db.Amenities
            .OrderBy(a => a.Name)
            .ToListAsync();

        model.AvailableAmenities = allAmenities.Select(a => new AmenityCheckboxItem
        {
            Id = a.Id,
            Name = a.Name,
            IsSelected = model.SelectedAmenityIds.Contains(a.Id)
        }).ToList();
    }
}
