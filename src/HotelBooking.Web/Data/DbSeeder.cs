using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedRoomTypesAsync(context);
        await SeedRoomsAsync(context);
        await SeedAmenitiesAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Admin", "Guest"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var adminEmail = "admin@purestay.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Hotel Admin",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var guestEmail = "guest@purestay.com";
        var guestUser = await userManager.FindByEmailAsync(guestEmail);
        if (guestUser == null)
        {
            guestUser = new ApplicationUser
            {
                UserName = guestEmail,
                Email = guestEmail,
                FullName = "Test Guest",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(guestUser, "Guest@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create guest user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(guestUser, "Guest"))
        {
            await userManager.AddToRoleAsync(guestUser, "Guest");
        }
    }

    private static async Task SeedRoomTypesAsync(ApplicationDbContext context)
    {
        var roomTypes = new (string Name, int Capacity, decimal BasePrice, string Description)[]
        {
            ("Standard", 2, 2500m, "A comfortable standard room suitable for up to two guests."),
            ("Deluxe", 3, 4000m, "A spacious deluxe room featuring upgraded amenities and furnishings."),
            ("Family Suite", 5, 6500m, "A large suite ideal for families, featuring multiple beds and living space.")
        };

        foreach (var rt in roomTypes)
        {
            if (!await context.RoomTypes.AnyAsync(t => t.Name == rt.Name))
            {
                context.RoomTypes.Add(new RoomType
                {
                    Name = rt.Name,
                    Capacity = rt.Capacity,
                    BasePrice = rt.BasePrice,
                    Description = rt.Description
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRoomsAsync(ApplicationDbContext context)
    {
        var roomTypesByName = await context.RoomTypes.ToDictionaryAsync(rt => rt.Name);

        var roomDefinitions = new (string RoomNumber, int Floor, string TypeName, decimal? PriceOverride)[]
        {
            ("101", 1, "Standard", null),
            ("102", 1, "Standard", null),
            ("103", 1, "Deluxe", null),
            ("201", 2, "Standard", null),
            ("202", 2, "Deluxe", null),
            ("203", 2, "Deluxe", null),
            ("301", 3, "Deluxe", null),
            ("302", 3, "Family Suite", 7200m)
        };

        var existingRooms = await context.Rooms.ToDictionaryAsync(r => r.RoomNumber);

        foreach (var def in roomDefinitions)
        {
            if (!existingRooms.ContainsKey(def.RoomNumber))
            {
                var room = new Room
                {
                    RoomNumber = def.RoomNumber,
                    Floor = def.Floor,
                    RoomTypeId = roomTypesByName[def.TypeName].Id,
                    ImageUrl = $"https://picsum.photos/seed/room{def.RoomNumber}/800/500",
                    PriceOverride = def.PriceOverride,
                    IsActive = true
                };

                context.Rooms.Add(room);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAmenitiesAsync(ApplicationDbContext context)
    {
        var amenityNames = new[] { "WiFi", "AC", "TV", "Breakfast", "Balcony", "Mini Fridge" };

        foreach (var name in amenityNames)
        {
            if (!await context.Amenities.AnyAsync(a => a.Name == name))
            {
                context.Amenities.Add(new Amenity { Name = name });
            }
        }

        await context.SaveChangesAsync();

        var amenitiesByName = await context.Amenities.ToDictionaryAsync(a => a.Name);
        var rooms = await context.Rooms.Include(r => r.RoomType).ToListAsync();

        var existingRoomAmenities = await context.RoomAmenities
            .Select(ra => new { ra.RoomId, ra.AmenityId })
            .ToListAsync();

        var existingLookup = new HashSet<(int RoomId, int AmenityId)>(
            existingRoomAmenities.Select(ra => (ra.RoomId, ra.AmenityId))
        );

        foreach (var room in rooms)
        {
            var targetAmenities = room.RoomType.Name switch
            {
                "Standard" => new[] { "WiFi", "AC", "TV" },
                "Deluxe" => new[] { "WiFi", "AC", "TV", "Breakfast", "Mini Fridge" },
                "Family Suite" => new[] { "WiFi", "AC", "TV", "Breakfast", "Balcony", "Mini Fridge" },
                _ => Array.Empty<string>()
            };

            foreach (var amenityName in targetAmenities)
            {
                if (amenitiesByName.TryGetValue(amenityName, out var amenity))
                {
                    if (!existingLookup.Contains((room.Id, amenity.Id)))
                    {
                        context.RoomAmenities.Add(new RoomAmenity
                        {
                            RoomId = room.Id,
                            AmenityId = amenity.Id
                        });

                        existingLookup.Add((room.Id, amenity.Id));
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }
}

