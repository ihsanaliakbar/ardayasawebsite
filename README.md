# Ardayasa Wellbeing and Growth Center — Website

Booking + marketing platform for a multi-psychologist counseling clinic (Bogor, Indonesia).
Spec: [docs/SPEC.md](docs/SPEC.md) · Decisions: [docs/DECISIONS.md](docs/DECISIONS.md)

## Stack

- **Frontend:** Angular (SSR on public routes) + Angular Material + ngx-translate (`client/`)
- **Backend:** ASP.NET Core Web API + EF Core + PostgreSQL (`server/`)
- **Auth:** ASP.NET Core Identity + JWT (in-memory access token, httpOnly rotated refresh cookie) + Google OAuth
- **Jobs:** Hangfire (Postgres storage)

## Run locally (full stack)

Prereqs: Docker Desktop.

```bash
cp .env.example .env   # then edit the change-me values
docker compose up --build
```

- Web: http://localhost:4200
- API: http://localhost:5000 (health: `/health`, Hangfire dashboard: `/hangfire`, Admin only)
- Postgres: localhost:5432

EF Core migrations apply automatically on API start in Development; a default admin
(`ADMIN_EMAIL` / `ADMIN_PASSWORD` from `.env`) and the marketing content (psychologist
profiles, service catalog, FAQ, testimonials, sample articles) are seeded on first run.
Content seeding is per-table and only runs when a table is empty, so admin edits are
never overwritten.

### Stop / restart

```bash
docker compose down             # stop (keeps the database)
docker compose up -d            # start again (no rebuild)
docker compose up --build -d    # start after code changes (rebuild images)
docker compose down -v          # full reset: deletes the database volume; re-seeds on next start
```

## Development (host)

Prereqs: .NET SDK (LTS), Node.js (LTS), Docker (for Postgres).

```bash
docker compose up postgres -d

# API
cd server
dotnet run --project Ardayasa.Api

# Web
cd client
npm install
npm start
```

## Tests

```bash
cd server
dotnet test
```

## Configuration

All secrets/config via environment variables — see [.env.example](.env.example).
Empty `Smtp__Host` / `Fonnte__ApiToken` switch email/WhatsApp to dev stubs that log
instead of sending. Empty `Google__ClientId` disables Google login with a clear error.

## Deployment notes

- **Add the production domain to `security.allowedHosts`** in [client/angular.json](client/angular.json)
  (hostname only, no port) — Angular SSR rejects requests from unlisted hosts.
- Apply EF migrations in production either by setting `AUTO_MIGRATE=true` once, or manually:
  `dotnet ef database update --project server/Ardayasa.Infrastructure --startup-project server/Ardayasa.Api`.
