# ExpenseTracker Server

A RESTful API backend for tracking bills to pay, built with ASP.NET Core 10 and Clean Architecture.

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

### Bills (JWT required)

The bill and recurring-bill endpoints are being built (see issue #29). The old `/expense` endpoints were removed.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance (local, Docker, or hosted)

## Environment Variables

The following environment variables must be set before running:

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL (Npgsql) connection string |
| `JWT_SECRET` | Secret key used to sign and validate JWT tokens (at least 32 bytes) |
| `JWT_ISSUER` | Optional. Token issuer (default `ExpenseTracker.API`) |
| `JWT_AUDIENCE` | Optional. Token audience (default `ExpenseTracker.Client`) |
| `JWT_EXPIRATION_MINUTES` | Optional. Token lifetime in minutes (default `43200`, 30 days) |
| `CORSOrigins` | Comma-separated list of allowed frontend origins, each as `scheme://host[:port]` (no path or trailing slash) |

## Getting Started

`ExpenseTracker.API/Properties/launchSettings.json` defines two profiles for
`dotnet run`: `http` (port 5149 only) and `https` (ports 7010/5149, with
Swagger). **`dotnet run` uses `http` by default** — pass `--launch-profile
https` explicitly to get HTTPS and match the Swagger URL below.

The `https` profile ships with `ConnectionStrings__DefaultConnection`/`CORSOrigins` set to
local defaults, but does not define `JWT_SECRET`, so no real secret is
committed to the repo. Export it in your shell as shown below before running
— a missing `JWT_SECRET`, or one shorter than 32 bytes, makes the app fail
fast on startup with an explicit message.

Variables defined in a launch profile **override** the ones exported in your
shell, so with `--launch-profile https` the exported
`ConnectionStrings__DefaultConnection` and `CORSOrigins` are replaced by the
profile's values. To use different values, edit the profile or run without it
(`--no-launch-profile`, in which case set `ASPNETCORE_ENVIRONMENT=Development`
yourself for Swagger). Do not add `JWT_SECRET` to the profile, not even
empty: an empty value would override the exported secret. Existing deployments only need to make
sure their secret is long enough; the other `JWT_*` variables have defaults.
Changing `JWT_ISSUER`/`JWT_AUDIENCE` invalidates tokens issued before the
change, and tokens issued before this validation existed (no issuer or
audience) are rejected, so users have to log in again once.

The connection string and `CORSOrigins` are validated the same way: if either
is missing or malformed, the app refuses to start and lists every problem
before touching the database. There is no built-in default connection string,
so it must always be set.

### Linux / macOS (bash)

```bash
# Clone the repo
git clone <repository-url>
cd ET-ExpenseTracker-Server

# Set environment variables (example for bash)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
export JWT_SECRET="your-secret-key-of-at-least-32-bytes"
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
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
$env:JWT_SECRET = "your-secret-key-of-at-least-32-bytes"
$env:CORSOrigins = "http://localhost:4200"

# Run the API with HTTPS + Swagger (database migrations are applied automatically on startup)
dotnet run --project ExpenseTracker.API --launch-profile https
```

`$env:` variables set this way only last for the current PowerShell session/terminal
window. To persist them across sessions, use `setx` instead (requires a new
terminal to take effect):

```powershell
setx ConnectionStrings__DefaultConnection "Host=localhost;Database=ExpenseTracker;Username=postgres;Password=postgres;"
setx JWT_SECRET "your-secret-key-of-at-least-32-bytes"
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
and grab the connection details. Build the connection string in .NET
format (note to get the connection string from: Direct Connection String, Connection method Session Pooler):

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zixxlxpnxvbyihflykme;Password=[YOUR-PASSWORD];SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### 2. Publish the image to GitHub Container Registry

`.github/workflows/docker.yml` builds the `Dockerfile` and pushes it to
`ghcr.io/brunostb/et-expensetracker-server` on every push to `main` (it can
also be started by hand from the Actions tab). Each build is tagged with the
commit SHA and with `latest`; the job summary prints the exact deploy command.
It authenticates with the workflow's `GITHUB_TOKEN`, so no secret is needed.

After the first run the image shows up under **Packages** on the GitHub
profile. Pick one:

- **Public package** (Package settings → Change visibility → Public): Azure
  pulls it without credentials. The image holds only the compiled app; secrets
  stay in the Container App's environment variables.
- **Private package**: create a classic personal access token with only the
  `read:packages` scope and pass it as the registry password below.

### 3. Deploy

```bash
az login

az group create --name expense-tracker-rg --location eastus

az containerapp env create --name expense-tracker-env --resource-group expense-tracker-rg --location eastus

az containerapp create \
  --name expense-tracker-api \
  --resource-group expense-tracker-rg \
  --environment expense-tracker-env \
  --image ghcr.io/brunostb/et-expensetracker-server:<commit-sha> \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 --max-replicas 1 \
  --env-vars \
    ConnectionStrings__DefaultConnection="Host=<your-host>;Database=<your-db>;Username=<your-user>;Password=<your-password>;Ssl Mode=Require;" \
    JWT_SECRET="<a random secret of at least 32 bytes — do not reuse the local dev value>" \
    JWT_ISSUER="https://your-api-domain.com" \
    JWT_AUDIENCE="https://your-frontend-domain.com" \
    CORSOrigins="https://your-frontend-domain.com"
```

For a private package, add
`--registry-server ghcr.io --registry-username <github-user> --registry-password <PAT>`
to `az containerapp create`.

Azure Container Apps terminates HTTPS for you and issues a free
`*.azurecontainerapps.io` subdomain automatically — no reverse proxy or
certificate setup needed. `--min-replicas 0` scales the app to zero when
idle, which is what keeps this within the free monthly compute grant; the
trade-off is a few seconds of cold-start latency on the first request after
idling. Set `--min-replicas 1` only if you want an always-warm instance —
that runs 24/7 and will likely exceed the free grant, incurring a small
monthly cost.

### 4. Redeploying after changes

Push to `main`, wait for the **Docker** workflow, then point the app at the new
commit:

```bash
az containerapp update --name expense-tracker-api --resource-group expense-tracker-rg \
  --image ghcr.io/brunostb/et-expensetracker-server:<commit-sha>
```

Use the SHA tag rather than `latest`: Container Apps only creates a new
revision when the image reference changes, and SHA tags make rollbacks a
matter of deploying an older SHA.

### Migrating an existing app from Azure Container Registry

Once the Docker workflow has published an image, switch the app over and then
delete the registry (the Basic SKU is billed even when unused):

```bash
# private package only
az containerapp registry set --name expense-tracker-api --resource-group expense-tracker-rg \
  --server ghcr.io --username <github-user> --password <PAT>

az containerapp update --name expense-tracker-api --resource-group expense-tracker-rg \
  --image ghcr.io/brunostb/et-expensetracker-server:<commit-sha>

# after confirming the new revision is healthy
az containerapp registry remove --name expense-tracker-api --resource-group expense-tracker-rg \
  --server <yourAcrName>.azurecr.io
az acr delete --name <yourAcrName> --resource-group expense-tracker-rg
```

## Authentication

Register a user, then call `/user/Login` to receive a JWT token. Include it in subsequent requests:

```
Authorization: Bearer <token>
```

Tokens are valid for **30 days**.
