# Pure Stay Hotel - Room Booking System

A hotel website where guests search for rooms by date and party size, book online and manage their bookings, and where staff manage rooms, room types and every booking.

## Tech stack

- ASP.NET Core MVC (.NET 8), C#
- SQL Server 2022 Express, Entity Framework Core 8 (code first, migrations)
- ASP.NET Core Identity with two roles, Admin and Guest
- Bootstrap 5 for the responsive UI

## Setup

1. Install the .NET 8 SDK and SQL Server Express (instance name SQLEXPRESS).
2. If your instance name differs, edit `DefaultConnection` in `src/HotelBooking.Web/appsettings.json`.
3. Create the database with one of these:
    - EF Core: from `src/HotelBooking.Web` run `dotnet ef database update`
    - Backup: restore `database/HotelBookingDB.bak` in SSMS
    - Script: run `database/HotelBookingDB.sql` (schema only)
4. From `src/HotelBooking.Web` run `dotnet run` and open the URL it prints.
5. On startup the app seeds roles, test users, room types, rooms and amenities if they are missing, so a fresh database fills itself.

## Test logins

| Role | Email | Password |
|---|---|---|
| Admin | admin@purestay.com | Admin@123 |
| Guest | guest@purestay.com | Guest@123 |

New guests can also register from the site.

## Design decisions

**Why these features.** A small hotel needs guests to find a free room and book it without calling, and staff to see who is arriving. So the guest side is search, room details, booking, and a My Bookings page with cancellation. The admin side is a dashboard (today's check-ins and check-outs), room and room type management, and a bookings list with filters and status changes. I kept the scope to what I could finish and test end to end.

**Business rules**
- Check-in cannot be in the past, check-out must be after check-in, maximum stay is 14 nights, bookings open up to one year ahead.
- Guest count cannot exceed the room type's capacity.
- A stay occupies the nights from check-in up to, but not including, check-out, so a guest can check in on the day another checks out.
- Total price is nights times the room's nightly rate (price override if set, otherwise the room type base price). It is saved on the booking, so later price changes do not alter old bookings.
- Guests can cancel their own bookings until 24 hours before 2 PM on the check-in day. Admins can cancel at any time.
- Admin status changes follow Confirmed, then Checked In, then Checked Out. Cancelled and Checked Out are final.
- A room with a future confirmed booking cannot be deleted. It can be deactivated instead.

**No double booking.** Overlap is checked with `existing.CheckIn < new.CheckOut AND existing.CheckOut > new.CheckIn`, ignoring cancelled bookings. The check and the insert run inside one transaction that first takes a row lock on the room, so two simultaneous requests for the same room queue up. The lock is needed because the database runs with READ_COMMITTED_SNAPSHOT on, where plain reads do not block each other.

**Database.** Constraints live in the database as well as the code: unique room number, booking reference and amenity name; check constraints for check-out after check-in, positive total and positive guest count; restrict-delete foreign keys; an index on (RoomId, CheckIn, CheckOut) for the availability search. See `database/ER-Diagram.png`.

**With more time:** payment simulation, confirmation emails, an availability calendar on the room page, room photo uploads, automated tests including a concurrent booking test, and pagination on the admin lists.

## Features

Done: guest search and booking, my bookings with cancel, registration and login, role based access, admin dashboard, room CRUD with amenities, room type CRUD, booking management with filters and status changes.

Not done: payments, emails, availability calendar, image upload (rooms use image URLs), automated tests.

## Known issues

- The overlap lock has been reasoned through but not load tested with truly parallel requests.
- Dates use the server's local time, so a hosted deployment would need a hotel time zone setting.
- Image URLs point to a placeholder service, so room photos need internet access.

## Folder layout

    src/          Visual Studio solution and project
    database/     HotelBookingDB.bak, HotelBookingDB.sql, ER-Diagram.png
    screenshots/  Desktop and mobile screenshots
    AI_TOOLS_USED.md

