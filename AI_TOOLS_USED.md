# AI tools used

| Tool | Version / plan | Used for |
|---|---|---|
| Antigravity (Google agentic IDE) with Gemini 3.8 Flash High | [add your plan] | Generating the entity classes and DbContext, the seed data class, the guest UI (controllers, view models, views) and the admin area |
| Claude (Anthropic) | [add your plan] | Planning scope and business rules, setup troubleshooting, drafting the booking service (overlap check, pricing, cancellation), reviewing the agent output, drafting these documents |

## Most useful prompts

1. "Add the domain layer: entities RoomType, Room, Amenity, RoomAmenity, Booking, ApplicationUser, and update ApplicationDbContext with unique indexes, decimal precision, check constraints (CheckOut > CheckIn, TotalPrice > 0, Guests > 0), Restrict deletes, and an index on Bookings (RoomId, CheckIn, CheckOut). Store the enum as a string. Do not create migrations."
2. "Add idempotent seed data: roles Admin and Guest, two test users, 3 room types, 8 rooms with images, 6 amenities linked to rooms. Make sure Identity is configured with AddRoles."
3. "Build the guest-facing UI with Bootstrap 5: search form, results using BookingService.SearchAvailableAsync, room details, booking flow with anti-forgery, confirmation page, My Bookings with cancel. Do not modify BookingService."
4. "Build the admin area under /Admin with [Authorize(Roles = "Admin")]: dashboard, room and room type CRUD, bookings list with filters and valid status transitions only. Block deleting rooms with future bookings."
5. "Write a booking service where the overlap check and insert happen in one transaction, and explain why a row lock is needed when READ_COMMITTED_SNAPSHOT is on."

## What I changed or fixed myself

- The .NET 8 template scaffolded SQLite, which does not meet the SQL Server requirement. I replaced the provider package, removed the SQLite database and migration, set the SQL Server connection string and generated a new migration.
- The EF Core command line tool was not installed, so I installed it before creating the database.
- I reviewed the generated DbContext line by line against the spec (DateOnly dates, check constraints, Restrict deletes, string enum) before committing.
- The two agents ran in parallel and one of them edited HomeController to remove an invalid attribute argument that broke the build. I rebuilt and tested the combined result before committing.
- I checked that the admin controllers carry their own routes, so no extra route was needed in Program.cs.
- I tested the double booking rule, the guest access block on /Admin and the admin pages by hand in the browser.
- [Add any other bug you hit or changed yourself.]
