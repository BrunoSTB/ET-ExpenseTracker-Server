# ExpenseTracker Server

A RESTful API backend for managing personal expenses, built with ASP.NET Core 9 and Clean Architecture.

## Tech Stack

- **.NET 9.0** — ASP.NET Core Web API
- **Entity Framework Core 9** — ORM with SQL Server (Azure SQL)
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

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- SQL Server instance (local or Azure SQL)

## Environment Variables

The following environment variables must be set before running:

| Variable | Description |
|----------|-------------|
| `SqlConnectionString` | SQL Server connection string |
| `JWT_SECRET` | Secret key used to sign JWT tokens |
| `CORSOrigins` | Comma-separated list of allowed frontend origins |

## Getting Started

```bash
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (example for bash)
export SqlConnectionString="Server=...;Database=ExpenseTracker;..."
export JWT_SECRET="your-secret-key"
export CORSOrigins="http://localhost:4200"

# Apply database migrations
dotnet ef database update --project ExpenseTracker.Infrastructure --startup-project ExpenseTracker.API

# Run the API
dotnet run --project ExpenseTracker.API
```

Swagger UI is available at `https://localhost:7010/swagger` when running in development.

## Authentication

Register a user, then call `/user/Login` to receive a JWT token. Include it in subsequent requests:

```
Authorization: Bearer <token>
```

Tokens are valid for **30 days**.
