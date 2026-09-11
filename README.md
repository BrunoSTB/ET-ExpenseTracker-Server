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

`ExpenseTracker.API/Properties/launchSettings.json` defines two profiles for
`dotnet run`: `http` (port 5149 only) and `https` (ports 7010/5149, with
Swagger). **`dotnet run` uses `http` by default** — pass `--launch-profile
https` explicitly to get HTTPS and match the Swagger URL below.

The `https` profile ships with `SqlConnectionString`/`CORSOrigins` set to
local defaults, but `JWT_SECRET` is intentionally left blank so no real
secret is committed to the repo. Fill it in locally (edit
`launchSettings.json` or export it as shown below) before running — an
empty `JWT_SECRET` makes the app fail fast on startup instead of silently
signing tokens with a known key.

### Linux / macOS (bash)

```bash
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (example for bash)
export SqlConnectionString="Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
export JWT_SECRET="your-secret-key"
export CORSOrigins="http://localhost:4200"

# Run the API with HTTPS + Swagger (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API --launch-profile https
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

# Run the API with HTTPS + Swagger (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API --launch-profile https
```

`$env:` variables set this way only last for the current PowerShell session/terminal
window. To persist them across sessions, use `setx` instead (requires a new
terminal to take effect):

```powershell
setx SqlConnectionString "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
setx JWT_SECRET "your-secret-key"
setx CORSOrigins "http://localhost:4200"
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

## Deploying to Azure Container Apps + Neon/Supabase

### 1. Create the Postgres database

Create a free project on [Neon](https://neon.tech) or [Supabase](https://supabase.com)
and grab the connection details. Build the `SqlConnectionString` in .NET
format (note to get the connection string from: Direct Connection String, Connection method Session Pooler):

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zixxlxpnxvbyihflykme;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### 2. Build and push the image, then deploy

```bash
az login

az group create --name expense-tracker-rg --location eastus

az acr create --resource-group expense-tracker-rg --name <yourAcrName> --sku Basic
az acr build --registry <yourAcrName> --image expense-tracker-api:latest .

az containerapp env create --name expense-tracker-env --resource-group expense-tracker-rg --location eastus

az containerapp create \
  --name expense-tracker-api \
  --resource-group expense-tracker-rg \
  --environment expense-tracker-env \
  --image <yourAcrName>.azurecr.io/expense-tracker-api:latest \
  --registry-server <yourAcrName>.azurecr.io \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 --max-replicas 1 \
  --env-vars \
    SqlConnectionString="Host=<your-host>;Database=<your-db>;Username=<your-user>;Password=<your-password>;Ssl Mode=Require;" \
    JWT_SECRET="<a long random secret — do not reuse the local dev value>" \
    CORSOrigins="https://your-frontend-domain.com"
```

Azure Container Apps terminates HTTPS for you and issues a free
`*.azurecontainerapps.io` subdomain automatically — no reverse proxy or
certificate setup needed. `--min-replicas 0` scales the app to zero when
idle, which is what keeps this within the free monthly compute grant; the
trade-off is a few seconds of cold-start latency on the first request after
idling. Set `--min-replicas 1` only if you want an always-warm instance —
that runs 24/7 and will likely exceed the free grant, incurring a small
monthly cost.

### 3. Redeploying after changes

```bash
az acr build --registry <yourAcrName> --image expense-tracker-api:latest .
az containerapp update --name expense-tracker-api --resource-group expense-tracker-rg \
  --image <yourAcrName>.azurecr.io/expense-tracker-api:latest
```

## Authentication

Register a user, then call `/user/Login` to receive a JWT token. Include it in subsequent requests:

```
Authorization: Bearer <token>
```

Tokens are valid for **30 days**.
