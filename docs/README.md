# Codex Darts – Demo Guide

This repository contains a minimal darts scoring experience with a Vue 3 frontend and a .NET 9 Web API backend. Docker Compose spins up SQL Server, the API, and the web UI with a single command.

## Prerequisites

* Docker Desktop 4.26+ (or Docker Engine + Docker Compose Plugin)
* Node.js 20+ and npm (optional – only required for running the frontend locally)
* .NET 9 SDK (optional – only required for running the API or unit tests locally)

## Quick start with Docker

1. Copy the environment template and adjust values if desired:
   ```bash
   cp .env.example .env
   ```
2. From the `infrastructure/` directory, build and launch everything:
   ```bash
   docker-compose up --build
   ```
3. Browse to `http://localhost:${WEB_PORT:-5173}` for the frontend and `http://localhost:${API_PORT:-5200}/swagger` for Swagger UI.

Docker Compose provisions:

* **sql** – SQL Server 2022 with seeded data (Alice, Bob, demo matches).
* **api** – ASP.NET Core Web API, applies EF Core migrations on startup, serves Swagger at `/swagger`.
* **web** – Built Vue app served by nginx. The app talks to the API via `http://api:5200` inside the network.

To stop the stack use `docker-compose down`. Add `-v` to drop the SQL volume if you want to reset seed data.

## Local development (optional)

### Backend

```bash
cd backend
cp ../.env.example .env # if you want to reuse the connection string variables
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=DartsScoring;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
dotnet restore
# Run migrations against your SQL Server instance (ensure it is running)
dotnet ef database update --project DartsScoring.Api
# Launch the API
cd DartsScoring.Api
dotnet run
```

Swagger is available at `https://localhost:5001/swagger` (or the HTTP port logged by Kestrel).

### Frontend

```bash
cd frontend/dart-scoring-frontend
npm install
npm run dev
```

Set `VITE_API_BASE_URL` in `.env` if your API runs on a non-default port.

### Tests

```bash
cd backend
dotnet test

cd ../frontend/dart-scoring-frontend
npm test
```

## Demo script

Once the containers are up:

1. **Open Match A (X01)**
   * Load the app and click **Match A – X01 Demo**.
   * Enter a few turns using the quick totals (e.g., 60, 45, 100) and show a bust scenario by overshooting the remaining score.
   * Watch the remaining score and 3-dart average update live.

2. **Start a fresh Cricket match**
   * Go back to **New Match**.
   * Create a new Cricket match with Alice and Bob (Bob starts by default).
   * Use the segment picker to mark 20s and 19s, demonstrating how points accrue once one side closes a number first.

3. **Undo a turn**
   * Click **Undo Last Turn** to remove the previous entry and confirm the scoreboard rolls back correctly.

4. **Show the summary**
   * Navigate to the **Summary** page to display per-turn breakdowns and player averages.

## API overview

* `GET /api/players` – List available players.
* `POST /api/matches` – Create a new match. Provide `mode`, `targetScore`, `doubleOut`, and two `playerIds`.
* `GET /api/matches/{id}` – Fetch match details with legs and current state.
* `POST /api/legs/{legId}/turns` – Submit up to three darts for the active player.
* `POST /api/turns/{turnId}/undo` – Undo the specified turn.
* `GET /api/legs/{legId}/summary` – Aggregated statistics per turn and player.

All endpoints return JSON and are documented in Swagger.

## Seed data

The initial migration seeds:

* Players: **Alice** (ID 1) and **Bob** (ID 2)
* Match 1: X01 (501, double-out) with Alice starting
* Match 2: Cricket with Bob starting

Use the demo shortcuts on the New Match page to load either seeded match instantly.

## Extending the demo

The codebase is organised as a monorepo:

```
/backend        # .NET Web API + EF Core
/frontend       # Vue 3 + Vite app
/infrastructure # Docker Compose
/docs           # Documentation (this file)
```

Both the frontend and backend were kept intentionally small so they can be extended later (e.g., automated scoring, additional game modes, authentication). Consult the Pinia store (`src/stores/matchStore.ts`) and the scoring service (`Services/ScoringService.cs`) as starting points for enhancements.
