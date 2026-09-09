# ExpenseTracker Server

A RESTful API backend for managing personal expenses, built with ASP.NET Core 9 and Clean Architecture.

## Tech Stack

- **.NET 10.0** — ASP.NET Core Web API
- **Entity Framework Core 10** — ORM with PostgreSQL
- **JWT** — Bearer token authentication
- **Swagger/OpenAPI** — Auto-generated API docs

## Architecture

Clean Architecture with four projects:

```
ExpenseTracker.API            — Controllers, middleware, startup
ExpenseTracker.Application    — Services, interfaces, business logic
ExpenseTracker.Domain         — Entities, DTOs
ExpenseTracker.Infrastructure — EF Core DbContext, repositories, migrations
```

## API Endpoints

### Auth (no JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/user/Register` | Register a new user |
| `POST` | `/user/Login` | Login and receive a JWT token |

### Users (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/user?id={id}` | Get user by ID |

### Expenses (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/expense?year={year}` | Get expenses by year, grouped by month |
| `POST` | `/expense` | Create a new expense |
| `DELETE` | `/expense/DeleteByIds?ids={ids}` | Delete expenses by comma-separated IDs |

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance (local, Docker, or hosted)

## Environment Variables

The following environment variables must be set before running:

| Variable | Description |
|----------|-------------|
| `SqlConnectionString` | PostgreSQL (Npgsql) connection string |
| `JWT_SECRET` | Secret key used to sign JWT tokens |
| `CORSOrigins` | Comma-separated list of allowed frontend origins |

## Getting Started

### Linux / macOS (bash)

```bash
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (example for bash)
export SqlConnectionString="Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
export JWT_SECRET="your-secret-key"
export CORSOrigins="http://localhost:4200"

# Run the API (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API
```

### Windows (PowerShell)

```powershell
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (current session only)
$env:SqlConnectionString = "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
$env:JWT_SECRET = "your-secret-key"
$env:CORSOrigins = "http://localhost:4200"

# Run the API (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API
```

`$env:` variables set this way only last for the current PowerShell session/terminal
window. To persist them across sessions, use `setx` instead (requires a new
terminal to take effect):

```powershell
setx SqlConnectionString "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
setx JWT_SECRET "your-secret-key"
setx CORSOrigins "http://localhost:4200"
```

Or set them per-run without touching the environment at all:

```powershell
$env:SqlConnectionString = "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"; `
$env:JWT_SECRET = "your-secret-key"; `
$env:CORSOrigins = "http://localhost:4200"; `
dotnet run --project ExpenseTracker.API
```

Swagger UI is available at `https://localhost:7010/swagger` when running in development.

## Running with Docker

The included `Dockerfile` and `docker-compose.yml` run the API and a PostgreSQL
database together — useful for self-hosting on a personal VPS:

```bash
docker compose up -d --build
```

The API applies pending EF Core migrations automatically on startup, so no
manual `dotnet ef database update` step is needed. Edit `JWT_SECRET` in
`docker-compose.yml` before deploying anywhere reachable from the internet.

## Authentication

Register a user, then call `/user/Login` to receive a JWT token. Include it in subsequent requests:

```
Authorization: Bearer <token>
```

Tokens are valid for **30 days**.
