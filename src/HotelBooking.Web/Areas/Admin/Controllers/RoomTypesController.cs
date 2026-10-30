using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Web.Areas.Admin.Models;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;

namespace HotelBooking.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[Route("Admin/[controller]")]
public class RoomTypesController : Controller
{
    private readonly ApplicationDbContext _db;

    public RoomTypesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var roomTypes = await _db.RoomTypes
            .Include(rt => rt.Rooms)
            .OrderBy(rt => rt.Name)
            .Select(rt => new RoomTypeListItemViewModel
            {
                Id = rt.Id,
                Name = rt.Name,
                Description = rt.Description,
                BasePrice = rt.BasePrice,
                Capacity = rt.Capacity,
                RoomsCount = rt.Rooms.Count
            })
            .ToListAsync();

        return View(roomTypes);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new RoomTypeFormViewModel
        {
            Capacity = 2,
            BasePrice = 2500m
        };
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomTypeFormViewModel model)
    {
        var nameTrimmed = model.Name?.Trim() ?? string.Empty;

        if (await _db.RoomTypes.AnyAsync(rt => rt.Name.ToLower() == nameTrimmed.ToLower()))
        {
            ModelState.AddModelError(nameof(model.Name), $"A room type with the name '{nameTrimmed}' already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var roomType = new RoomType
        {
            Name = nameTrimmed,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            BasePrice = model.BasePrice,
            Capacity = model.Capacity
        };

        _db.RoomTypes.Add(roomType);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room type '{roomType.Name}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var roomType = await _db.RoomTypes.FindAsync(id);
        if (roomType == null)
        {
            TempData["Error"] = "Room type not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new RoomTypeFormViewModel
        {
            Id = roomType.Id,
            Name = roomType.Name,
            Description = roomType.Description,
            BasePrice = roomType.BasePrice,
            Capacity = roomType.Capacity
        };

        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomTypeFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var roomType = await _db.RoomTypes.FindAsync(id);
        if (roomType == null)
        {
            TempData["Error"] = "Room type not found.";
            return RedirectToAction(nameof(Index));
        }

        var nameTrimmed = model.Name?.Trim() ?? string.Empty;

        if (await _db.RoomTypes.AnyAsync(rt => rt.Name.ToLower() == nameTrimmed.ToLower() && rt.Id != id))
        {
            ModelState.AddModelError(nameof(model.Name), $"A room type with the name '{nameTrimmed}' already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        roomType.Name = nameTrimmed;
        roomType.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        roomType.BasePrice = model.BasePrice;
        roomType.Capacity = model.Capacity;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Room type '{roomType.Name}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
