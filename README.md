# Ultimate Tic Tac Toe

A domain-driven, event-sourced implementation of Ultimate Tic Tac Toe with a clean separation between API, Core, and Storage.
Built on .NET 10 — a playable game backend and a reference architecture for modern DDD/CQRS patterns.

---

## Project Goals

- Implement Ultimate Tic Tac Toe with rule-enforced gameplay
- Leverage Event Sourcing for state reconstruction and reconnect
- Expose a clean HTTP + SignalR API for lobby, games, and moves
- Keep the system scalable, testable, and observable

---

## Tech Stack

- **.NET 10** / **ASP.NET Core Web API**
- **MongoDB** for event store, snapshots, and rooms/matchmaking
- **SignalR** for move updates and rooms notifications
- **MediatR** + `Result<T>` for handlers and API responses
- **xUnit** (+ WebApplicationFactory smoke tests, storage integration tests)

---

## Features

- Start games and make legal moves on a 9×9 (3×3 of 3×3) board
- Event persistence + replay (restart-safe rehydration)
- Snapshots for faster load (threshold / terminal events)
- Paginated moves history for reconnect
- Rooms + matchmaking (regular queue and private join codes)
- Capacity backpressure (HTTP 429) near `MaxActiveGames`
- Health checks (liveness / readiness) with JSON responses
- Correlation id middleware (`X-Correlation-Id`)
- OpenAPI + Scalar in Development

---

## Custom Logic / Conventions

- **Event sourcing + replay (restart-safe)**:
  - Game creation and moves are persisted as domain events via `IEventStore` (Mongo).
  - If a game isn’t in memory, it is rehydrated from snapshot + delta events (or full replay).
  - Key code: `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`, `src/UltimateTicTacToe.Storage/Services/MongoEventStore.cs`

- **Moves history is derived from events**:
  - `GET /api/game-management/{gameId}/moves-history?skip&take` reads persisted `CellMarkedEvent`s by version.
  - Key code: `src/UltimateTicTacToe.Core/Features/GameManagement/GetMovesHistoryQueryHandler.cs`

- **Capacity backpressure (near-capacity throttling)**:
  - Hard cap: `GameplaySettings.MaxActiveGames`
  - Threshold: \(\lceil MaxActiveGames \cdot BackpressureThresholdPercent / 100 \rceil\)
  - When `activeGames >= threshold`, new admissions return **HTTP 429** (game start, rooms queue, private create, private join).
  - Defaults (base `appsettings.json`): `MaxActiveGames=140`, `BackpressureThresholdPercent=90` ⇒ threshold **126**.

- **Rooms + matchmaking (pre-game)**:
  - Regular: queue → pair → game created; room/ticket cleaned up.
  - Private: create → `joinCode` → second player joins → game created.
  - TTL + expiry sweeper; SignalR hub `/rooms-hub` (`QueueJoined`, `MatchFound`, `PrivateRoomCreated`, `QueueExpired`, `RoomExpired`).
  - Config: `RoomSettings`, `RoomsSweepSettings`.

- **Health JSON**:
  - `/health/live`, `/health/ready`, `/health` return JSON `{ status, checks: { name: { status } } }` (no check descriptions — avoids leaking Mongo errors).
  - Mongo ping is gated by `HealthChecks:MongoEnabled` (false in base/Testing; true in Development/Production).

- **Production config**:
  - Inject Mongo via env: `EventStoreSettings__ConnectionString` (not committed).
  - CORS allowlist is HTTPS hostnames only (see `appsettings.Production.json`).

Frontend contract reference: [docs/USER_FLOWS.md](docs/USER_FLOWS.md). Roadmap: [docs/ROADMAP.md](docs/ROADMAP.md).

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (local Compose scripts available for Rider/dev)

### Run the API

```bash
dotnet run --project src/UltimateTicTacToe.Api --environment Development --urls http://localhost:8080
```

Open Scalar: `http://localhost:8080/scalar/v1`

Docker image listens on **8080** (`ASPNETCORE_URLS=http://0.0.0.0:8080`).

API smoke tests use environment **Testing** (`appsettings.Testing.json` + in-memory doubles).

### HTTP Endpoints

| Method | URL | Purpose |
|--------|-----|---------|
| POST | `/api/game/start` | Start a new game |
| POST | `/api/game/move` | Make a move |
| DELETE | `/api/game-management/clear` | Clear finished games from memory |
| GET | `/api/game-management/{gameId}/moves-history?skip&take` | Paginated move history |
| POST | `/api/rooms/queue` | Join regular matchmaking (`X-User-Id`) |
| DELETE | `/api/rooms/queue/{ticketId}` | Leave queue |
| POST | `/api/rooms/private` | Create private room |
| POST | `/api/rooms/private/join/{joinCode}` | Join private room |
| GET | `/api/rooms/me` | Current user’s room/ticket |
| GET | `/api/rooms/private/{joinCode}` | Private room state |
| GET | `/api/metrics/unfinished-games` | In-memory unfinished games |
| GET | `/api/metrics/rooms` | Rooms/queue metrics |
| GET | `/health/live` | Liveness (deploy probe) |
| GET | `/health/ready` | Readiness (incl. Mongo when enabled) |
| GET | `/health` | All health checks |

### SignalR

| Hub | Path |
|-----|------|
| Move updates | `/move-updates-hub` |
| Rooms | `/rooms-hub` |

### Testing

```bash
dotnet test
```

### Project Structure

```
src/UltimateTicTacToe.Api/       # Controllers, hubs, middleware, health, Program.cs
src/UltimateTicTacToe.Core/      # Domain, features (gameplay, rooms, metrics), in-memory repo
src/UltimateTicTacToe.Storage/   # Mongo event store, snapshots, rooms stores, hosted services
tests/UltimateTicTacToe.API.Tests.Unit/
tests/UltimateTicTacToe.Core.Tests.Unit/
tests/UltimateTicTacToe.Core.Tests.Integration/
tests/UltimateTicTacToe.Storage.Tests.Integration/
docs/                            # ROADMAP, USER_FLOWS, frontend learning plan
```

### Contributions

Feel free to fork, file issues, or submit PRs. Feedback and feature requests are welcome.

### License

This project is licensed under the MIT License.
