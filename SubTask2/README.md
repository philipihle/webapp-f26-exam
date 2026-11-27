# SubTask 2 – Study Room Booking Tool (ASP.NET 10.0 API + React SPA)

SubTask 2 has two parts that run side by side:

| Folder | What it is | URL |
|---|---|---|
| `api/` | ASP.NET 10.0 Web API with EF Core (SQLite), Identity and JWT | http://localhost:5102 |
| `roomBookSub2/` | React 19 + TypeScript SPA built with Vite | http://localhost:5173 |

## Requirements

- .NET 10 SDK
- Node.js 20 or newer (with npm)

## Running the Application

Start the API and the frontend in two separate terminals.

**1. API** (from the `SubTask2/api` folder):

```bash
dotnet run
```

The database `StudyRoomApi.db` is included with test data, so no migrations need to be run.

**2. Frontend** (from the `SubTask2/roomBookSub2` folder):

```bash
npm install
npm run dev
```

Open http://localhost:5173 in the browser. The frontend reads the API address from `.env.development` (`VITE_API_URL=http://localhost:5102`).

## Login Information

| Role | Email | Password |
|---|---|---|
| Admin | `admin@booking.com` | `Admin123` |
| Student | `student@booking.com` | `Student123` |

New student accounts can also be created via **Register**.

## Features

- **Rooms:** browse rooms as cards or a table, search by name/description and filter by building. Admins can create, update and delete rooms.
- **Bookings:** book a room with date, time, purpose and whether others may join. Server-side rules: no overlapping bookings, max 4 hours, 15-minute intervals, not in the past. Owners can cancel and switch a booking between open and private.
- **Participants:** students can join other students' open bookings from the room's booking page and leave them again from **My Bookings**.

## Structure

```
api/
  Controllers/   RoomAPI, BookingAPI, BookingParticipantAPI, Auth
  DAL/           MyDbContext and repositories (interface + implementation)
  DTOs/          Data sent between API and frontend
  Models/        Room, Booking, BookingParticipant
roomBookSub2/src/
  auth/          AuthContext, login and register pages
  bookings/      Booking pages and services
  rooms/         Room pages, grid/table and service
  shared/        NavMenu and Footer
  types/         TypeScript types matching the DTOs
```