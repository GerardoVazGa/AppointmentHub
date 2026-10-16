# AppointmentHub.Api

A REST API for managing services, business hours, availability, and bookings for an appointment-based business. It is built with ASP.NET Core and Entity Framework Core, and uses PostgreSQL as its database.

## Requirements

- .NET SDK 10.0
- Docker and Docker Compose, or your own PostgreSQL instance
- `dotnet-ef` 10.0.12 to apply migrations from the command line

## Setup and Run

1. Start PostgreSQL with Docker Compose:

   ```powershell
   docker compose up -d db
   ```

   The local configuration creates the `appointmenthub` database with the username and password `postgres`, and publishes it on port `5433`.

2. Set the connection string as a development secret:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=appointmenthub;Username=postgres;Password=postgres" --project AppointmentHub.Api.csproj
   ```

   The API reads `ConnectionStrings:Default`. No connection string is included in the `appsettings` files.

3. Install the EF Core tool if it is not already installed, then apply the migrations:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.12
   dotnet ef database update --project AppointmentHub.Api.csproj
   ```

   If `dotnet-ef` is already installed, you can skip the installation. Migrations are not applied automatically when the API starts.

4. Start the API:

   ```powershell
   dotnet run --project AppointmentHub.Api.csproj --launch-profile http
   ```

   The configured local URL is `http://localhost:5180`. In the `Development` environment, OpenAPI is available at `http://localhost:5180/openapi/v1.json`.

## Endpoints

All resources are served under `/api`. Requests and responses use JSON.

### Services

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/services` | Lists services |
| `GET` | `/api/services/{id}` | Gets a service; returns `404` if it does not exist |
| `POST` | `/api/services` | Creates a service |
| `PUT` | `/api/services/{id}` | Updates a service |
| `DELETE` | `/api/services/{id}` | Deletes a service |

Example request body:

```json
{
  "name": "Haircut",
  "durationMinutes": 30,
  "price": 150
}
```

### Business Hours

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/working-hours` | Lists configured business hours |
| `POST` | `/api/working-hours` | Creates business hours for a day |
| `PUT` | `/api/working-hours/{id}` | Updates business hours |
| `DELETE` | `/api/working-hours/{id}` | Deletes business hours |

Send the day as a .NET `DayOfWeek` enum name and times in `HH:mm:ss` format:

```json
{
  "dayOfWeek": "Monday",
  "startTime": "09:00:00",
  "endTime": "17:00:00"
}
```

Only one business-hours entry is allowed per day, and the start time must be earlier than the end time.

### Availability

| Method | Route | Parameters | Description |
| --- | --- | --- | --- |
| `GET` | `/api/availability` | `serviceId`, `date` (`YYYY-MM-DD`) | Returns whether the business is open that day and, if so, the available start times for the service |

Example: `GET /api/availability?serviceId=1&date=2026-10-19`. Slots are generated in increments equal to the service duration, excluding those that overlap with confirmed bookings.

### Bookings

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/bookings` | Lists bookings; optionally filters by `date=YYYY-MM-DD` |
| `POST` | `/api/bookings` | Creates a booking if the service exists and it does not overlap a confirmed booking |
| `POST` | `/api/bookings/{id}/cancel` | Cancels a confirmed booking |

Example request body:

```json
{
  "customerName": "Ana Perez",
  "serviceId": 1,
  "startTime": "2026-10-19T09:00:00Z"
}
```

The selected service determines `endTime` based on its duration. Booking dates are stored as timestamps with time zone; send `startTime` in UTC with the `Z` suffix. Booking status is represented as `Confirmed` or `Cancelled`.

## Testing the API

The [`AppointmentHub.Api.http`](AppointmentHub.Api.http) file contains a sample request for creating a service. It can be run from VS Code with an extension that supports `.http` files, once the API and PostgreSQL are running.

Run the tests with:

```powershell
dotnet test AppointmentHub.Tests/AppointmentHub.Tests.csproj
```

The current tests cover available start-time calculation and the exclusion of overlapping slots.

## Project Structure

- `Controllers/`: HTTP endpoints for services, business hours, availability, and bookings.
- `Domain/`: domain entities, booking statuses, and availability calculation.
- `Data/AppDbContext.cs`: Entity Framework Core database context.
- `Migrations/`: PostgreSQL schema change history.
- `AppointmentHub.Tests/`: unit tests for availability calculation.

## Design Notes

- **Overlap rule:** a new booking is rejected with `409` if it starts before an existing confirmed booking ends and ends after it starts. Back-to-back bookings are allowed, and cancelled bookings free the slot.
- **Validation:** invalid input (for example, an end time before the start time) returns `400`; conflicts with existing data return `409`.
- **Time handling:** booking dates are stored in UTC, and business hours are interpreted as UTC.

## Known Limitations

- Overlap checking is not safe against simultaneous requests; a database-level constraint would be needed.
- A single booking calendar: every booking blocks the time for all services.
- No authentication or roles yet.

## Current Scope

This version does not configure user authentication or authorization. It also does not seed sample data; services and business hours must be created through the API before checking availability.