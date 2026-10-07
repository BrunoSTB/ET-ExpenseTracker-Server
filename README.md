# ExpenseTracker Server

A RESTful API backend for managing personal expenses, built with ASP.NET Core 10 and Clean Architecture.

## Tech Stack

- **.NET 10.0** — ASP.NET Core Web API
- **Entity Framework Core 10** — ORM with PostgreSQL
- **JWT** — Bearer token authentication
- **Swagger/OpenAPI** — Auto-generated API docs

## Architecture

Clean Architecture with four projects:

```
ExpenseTracker.API            — Controllers, DTOs, middleware, startup
ExpenseTracker.Application    — Services, repository interfaces
ExpenseTracker.Domain         — Entities
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
| `GET` | `/user/me` | Get the authenticated user |

### Expenses (JWT required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/expense?year={year}` | Get expenses by year, grouped by month |
| `POST` | `/expense` | Create a new expense |
| `DELETE` | `/expense/DeleteByIds?ids={ids}` | Delete expenses by comma-separated IDs |

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance (local, Docker, or hosted)

## Configuration

Configuration is bound to typed options (`ExpenseTracker.API/Configuration`)
through the standard ASP.NET Core configuration system, so every setting can
come from `appsettings.json`, environment variables (use `__` as the section
separator), user secrets, etc. All options are validated on startup: a missing
or invalid value stops the app immediately with a message naming the setting.

| Setting | Environment variable | Required | Description |
|---------|----------------------|----------|-------------|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | yes | PostgreSQL (Npgsql) connection string. There is no fallback. |
| `Jwt:Secret` | `Jwt__Secret` | yes | Key used to sign and validate JWT tokens (HS256). Must be at least **32 bytes**. |
| `Jwt:Issuer` | `Jwt__Issuer` | default in `appsettings.json` (`ExpenseTracker.API`) | Token issuer; validated on every request. |
| `Jwt:Audience` | `Jwt__Audience` | default in `appsettings.json` (`ExpenseTracker.Client`) | Token audience; validated on every request. |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | default in `appsettings.json` (`10080`, 7 days) | Token lifetime, between 1 and 43200 (30 days). |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins` | yes | Comma-separated list of allowed frontend origins, as `scheme://host[:port]` without a trailing slash. |

Generate a secret with e.g. `openssl rand -base64 48`.

### Migrating from the old variable names

The previous ad-hoc variables are no longer read. Rename them in every
environment (local shell, `launchSettings.json`, Docker, Azure):

| Old | New |
|-----|-----|
| `SqlConnectionString` | `ConnectionStrings__Default` |
| `JWT_SECRET` | `Jwt__Secret` (now must be ≥ 32 bytes) |
| `CORSOrigins` | `Cors__AllowedOrigins` (no trailing slash) |

Tokens now carry and are validated against an issuer and audience, so tokens
issued before this change are rejected and users have to log in again.

## Getting Started

`ExpenseTracker.API/Properties/launchSettings.json` defines two profiles for
`dotnet run`: `http` (port 5149 only) and `https` (ports 7010/5149, with
Swagger). **`dotnet run` uses `http` by default** — pass `--launch-profile
https` explicitly to get HTTPS and match the Swagger URL below.

The `https` profile ships with `ConnectionStrings__Default`/`Cors__AllowedOrigins`
set to local defaults, but `Jwt__Secret` is intentionally left blank so no real
secret is committed to the repo. Fill it in locally (edit
`launchSettings.json`, export it as shown below, or use
`dotnet user-secrets set "Jwt:Secret" "<secret>" --project ExpenseTracker.API`)
before running — an empty or short secret makes the app fail fast on startup
instead of silently signing tokens with a weak key.

### Linux / macOS (bash)

```bash
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (example for bash)
export ConnectionStrings__Default="Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
export Jwt__Secret="$(openssl rand -base64 48)"
export Cors__AllowedOrigins="http://localhost:4200"

# Run the API with HTTPS + Swagger (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API --launch-profile https
```

### Windows (PowerShell)

```powershell
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (current session only)
$env:ConnectionStrings__Default = "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
$env:Jwt__Secret = "<a random secret of at least 32 bytes>"
$env:Cors__AllowedOrigins = "http://localhost:4200"

# Run the API with HTTPS + Swagger (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API --launch-profile https
```

`$env:` variables set this way only last for the current PowerShell session/terminal
window. To persist them across sessions, use `setx` instead (requires a new
terminal to take effect):

```powershell
setx ConnectionStrings__Default "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
setx Jwt__Secret "<a random secret of at least 32 bytes>"
setx Cors__AllowedOrigins "http://localhost:4200"
```

Swagger UI is available at `https://localhost:7010/swagger` when running in development.

## Running with Docker

The included `Dockerfile` and `docker-compose.yml` run the API and a PostgreSQL
database together — useful for self-hosting on a personal VPS:

```bash
docker compose up -d --build
```

The API applies pending EF Core migrations automatically on startup, so no
manual `dotnet ef database update` step is needed. Edit `Jwt__Secret` in
`docker-compose.yml` (at least 32 bytes) before deploying anywhere reachable
from the internet.

## Deploying to Azure Container Apps + Neon/Supabase

### 1. Create the Postgres database

Create a free project on [Neon](https://neon.tech) or [Supabase](https://supabase.com)
and grab the connection details. Build the `ConnectionStrings__Default` value in .NET
format (note to get the connection string from: Direct Connection String, Connection method Session Pooler):

```
Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zixxlxpnxvbyihflykme;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true
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
    ConnectionStrings__Default="Host=<your-host>;Database=<your-db>;Username=<your-user>;Password=<your-password>;Ssl Mode=Require;" \
    Jwt__Secret="<a random secret of at least 32 bytes — do not reuse the local dev value>" \
    Cors__AllowedOrigins="https://your-frontend-domain.com"
```

Azure Container Apps terminates HTTPS for you and issues a free
`*.azurecontainerapps.io` subdomain automatically — no reverse proxy or
certificate setup needed. `--min-replicas 0` scales the app to zero when
idle, which is what keeps this within the free monthly compute grant; the
trade-off is a few seconds of cold-start latency on the first request after
idling. Set `--min-replicas 1` only if you want an always-warm instance —
that runs 24/7 and will likely exceed the free grant, incurring a small
monthly cost.

If the app was deployed with the old variable names, rename them before
redeploying (otherwise the new image fails on startup):

```bash
az containerapp update --name expense-tracker-api --resource-group expense-tracker-rg \
  --remove-env-vars SqlConnectionString JWT_SECRET CORSOrigins \
  --set-env-vars \
    ConnectionStrings__Default="<connection string>" \
    Jwt__Secret="<secret of at least 32 bytes>" \
    Cors__AllowedOrigins="https://your-frontend-domain.com"
```

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

Tokens are valid for `Jwt:ExpirationMinutes` (**7 days** by default) and are
validated against the configured issuer and audience.
