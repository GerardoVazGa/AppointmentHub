# AppointmentHub.Api

A REST API for managing services, business hours, availability, and bookings for an appointment-based business. It is built with ASP.NET Core and Entity Framework Core, uses PostgreSQL as its database, and secures its endpoints with JWT bearer tokens and two roles (`Customer` and `Admin`).

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

3. Set the JWT signing key as a development secret (use any random string of 32 or more characters):

   ```powershell
   dotnet user-secrets set "Jwt:Key" "your-random-key-of-at-least-32-characters" --project AppointmentHub.Api.csproj
   ```

   The token issuer and audience are configured in `appsettings.json`. The signing key is never committed to the repository.

4. Install the EF Core tool if it is not already installed, then apply the migrations:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.12
   dotnet ef database update --project AppointmentHub.Api.csproj
   ```

   If `dotnet-ef` is already installed, you can skip the installation. Migrations are not applied automatically when the API starts.

5. Start the API:

   ```powershell
   dotnet run --project AppointmentHub.Api.csproj --launch-profile http
   ```

   The configured local URL is `http://localhost:5180`. In the `Development` environment, OpenAPI is available at `http://localhost:5180/openapi/v1.json`.

## Authentication

The API uses JWT bearer tokens and two roles: `Customer` and `Admin`.

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Public | Creates a user |
| `POST` | `/api/auth/login` | Public | Returns a token valid for 2 hours |
| `GET` | `/api/auth/me` | Any logged-in user | Returns the authenticated user's id, email and role |

- Every new user is a `Customer`. The role cannot be chosen in the registration request.
- Send the token on protected requests as `Authorization: Bearer <token>`.
- Passwords are stored as hashes (PBKDF2 through ASP.NET Core's `PasswordHasher`), never in plain text, and are never returned by the API.
- Login returns the same `401` response for an unknown email and a wrong password.

Example registration body:

```json
{
  "name": "Ana López",
  "email": "ana@example.com",
  "password": "at-least-8-characters"
}
```

### Creating the first admin

Admins cannot be created through the API. Promote a registered user directly in the database:

```sql
UPDATE "Users" SET "Role" = 'Admin' WHERE "Email" = 'ana@example.com';
```

The role is stored inside the token, so log in again to get a token with the new role.

## Endpoints

All resources are served under `/api`. Requests and responses use JSON. The **Access** column shows who can call each endpoint.

### Services

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/api/services` | Public | Lists services |
| `GET` | `/api/services/{id}` | Public | Gets a service; returns `404` if it does not exist |
| `POST` | `/api/services` | Admin | Creates a service |
| `PUT` | `/api/services/{id}` | Admin | Updates a service |
| `DELETE` | `/api/services/{id}` | Admin | Deletes a service |

Example request body:

```json
{
  "name": "Haircut",
  "durationMinutes": 30,
  "price": 150
}
```

### Business Hours

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/api/working-hours` | Admin | Lists configured business hours |
| `POST` | `/api/working-hours` | Admin | Creates business hours for a day |
| `PUT` | `/api/working-hours/{id}` | Admin | Updates business hours |
| `DELETE` | `/api/working-hours/{id}` | Admin | Deletes business hours |

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

| Method | Route | Access | Parameters | Description |
| --- | --- | --- | --- | --- |
| `GET` | `/api/availability` | Public | `serviceId`, `date` (`YYYY-MM-DD`) | Returns whether the business is open that day and, if so, the available start times for the service |

Example: `GET /api/availability?serviceId=1&date=2026-10-19`. Start times are generated in increments equal to the service duration, excluding those that overlap with confirmed bookings.

### Bookings

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/api/bookings` | Logged-in user | Lists bookings: admins see all of them, customers only their own. Optionally filters by `date=YYYY-MM-DD` |
| `POST` | `/api/bookings` | Logged-in user | Creates a booking for the authenticated user if the service exists and it does not overlap a confirmed booking |
| `POST` | `/api/bookings/{id}/cancel` | Booking owner or admin | Cancels a confirmed booking |

Example request body:

```json
{
  "serviceId": 1,
  "startsAt": "2026-10-19T09:00:00Z"
}
```

The customer name is taken from the authenticated user, and the selected service determines the end time based on its duration. Booking dates are stored as timestamps with time zone; send `startsAt` in UTC with the `Z` suffix.

Example response:

```json
{
  "id": 1,
  "customerName": "Ana López",
  "startTime": "2026-10-19T09:00:00Z",
  "endTime": "2026-10-19T09:30:00Z",
  "serviceId": 1,
  "serviceName": "Haircut",
  "userId": 1,
  "status": "Confirmed"
}
```

Booking status is represented as `Confirmed` or `Cancelled`.

## Testing the API

The [`AppointmentHub.Api.http`](AppointmentHub.Api.http) file contains sample requests. It can be run from VS Code with an extension that supports `.http` files, once the API and PostgreSQL are running.

Run the tests with:

```powershell
dotnet test AppointmentHub.Tests/AppointmentHub.Tests.csproj
```

The current tests cover available start-time calculation and the exclusion of overlapping slots.

## Project Structure

- `Controllers/`: HTTP endpoints for authentication, services, business hours, availability, and bookings.
- `Domain/`: domain entities (including users and roles), booking statuses, and availability calculation.
- `Data/AppDbContext.cs`: Entity Framework Core database context.
- `Migrations/`: PostgreSQL schema change history.
- `AppointmentHub.Tests/`: unit tests for availability calculation.

## Design Notes

- **Overlap rule:** a new booking is rejected with `409` if it starts before an existing confirmed booking ends and ends after it starts. Back-to-back bookings are allowed, and cancelled bookings free the slot.
- **Validation:** invalid input (for example, an end time before the start time) returns `400`; conflicts with existing data return `409`.
- **Authentication:** a missing or invalid token returns `401`; a valid token without permission for the resource returns `403`.
- **Access control:** customers can only list and cancel their own bookings; admins manage services and business hours and can see and cancel any booking.
- **Response models:** bookings are returned through a dedicated response record instead of the entity, so user data such as password hashes is never exposed.
- **Time handling:** booking dates are stored in UTC, and business hours are interpreted as UTC.

## Known Limitations

- Overlap checking is not safe against simultaneous requests; a database-level constraint would be needed.
- A single booking calendar: every booking blocks the time for all services.
- Tokens cannot be revoked, and there are no refresh tokens or password reset.

## Current Scope

This version does not seed sample data; services and business hours must be created through the API before checking availability. The first admin must be promoted manually in the database.