# Ultimate Tic Tac Toe Game Roadmap

This roadmap outlines the planned development and milestones for the Ultimate Tic Tac Toe game project.

Sibling frontend: `ultimate-tic-tac-toe-front` (live lobby/rooms wired; Game page still mock). See [../ultimate-tic-tac-toe-front/ROADMAP.md](../../ultimate-tic-tac-toe-front/ROADMAP.md) when working from a monorepo checkout, or the front repo `ROADMAP.md`.

---

## Completed

- [x] Core domain model (`GameRoot`, `Board`, `Cell`)
- [x] In-memory `IGameRepository` (basic start game + make move)
- [x] Semaphore-protected concurrent game management
- [x] `GameplayController` with HTTP endpoints
- [x] Result pattern for API responses
- [x] xUnit tests for repository and controller
- [x] Configuration-driven gameplay settings via JSON
- [x] Logging and diagnostics for moves and game lifecycle
- [x] Integration with MongoDB for event store
- [x] MongoDB event store + integration tests (storage layer)
- [x] Migrated solution + tests to **.NET 10** (`net10.0` only)
- [x] API docs via **OpenAPI + Scalar** (development only)
- [x] Coverage report generation (`coverlet` + `reportgenerator`) + gitignore for artifacts
- [x] Local dev Docker dependencies for Mongo (Docker Compose + Rider-friendly scripts)
- [x] HTTP Endpoint for Sending Moves to Server
- [x] HTTP Endpoint for Initial Move History (With Pagination)
- [x] WebSocket Hub for keeping up with Real-Time server's updates
- [x] Snapshotting mechanism wired end-to-end (repo uses snapshots + clears uncommitted changes)
- [x] Event persistence + replay wired end-to-end (repo appends events, rehydrates on demand)
- [x] Integration tests for controller/API surface (basic API smoke tests)
- [x] Rooms + matchmaking (Regular/Private) + TTL + metrics + expiry notifications
- [x] Rooms read endpoints (`GET /api/rooms/me`, `GET /api/rooms/private/{joinCode}`)
- [x] Capacity backpressure (429) for new admissions near `MaxActiveGames` + documented in README
- [x] Health checks (`/health/live`, `/health/ready`, `/health`) + optional Mongo ping (configurable)
- [x] JSON health responses (`HealthCheckJsonWriter`: status + check statuses, no descriptions)
- [x] Correlation id middleware (`X-Correlation-Id`) + logging scope propagation
- [x] CORS policy: origins (+ policy name) from `CorsSettings`; methods/headers/`AllowCredentials` hardcoded in `Program.cs`
- [x] Testing + Production appsettings overlays; shared gameplay/room defaults in base `appsettings.json`
- [x] Container listens on **8080** (Dockerfile + Container launch profile aligned)
- [x] `DELETE /api/game-management/clear` — clears finished games from memory only (events retained)
- [x] Production pairing path is rooms matchmaking → `TryStartGameForPlayersAsync`; legacy `POST /api/game/start` still creates ephemeral player IDs

---

## In Progress

_(none — next shared work is front Game page live path / move hub; see Frontend P1)_

---

## Suggested Next Steps (Agent Review)

These are recommendations based on the current repo state (API/Core/Storage/tests) and what’s most likely to unblock the next features.

### P0 (Next sprint / unblockers)

- [x] Wire **event persistence** into `IGameRepository` (append `GameRoot.UncommittedChanges` to `IEventStore`)
  - DoD:
    - After a successful move/start, events are appended via `IEventStore.AppendEventsAsync(...)`
    - `GameRoot.ClearUncomittedEvents(...)` is called after persistence
    - Basic optimistic/concurrency safety story is defined (even if “single node lock” for now)
  - Touchpoints: `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`, `src/UltimateTicTacToe.Core/Domain/Aggregate/GameRoot.cs`, `src/UltimateTicTacToe.Storage/Services/MongoEventStore.cs`

- [x] Implement **move history endpoint** end-to-end (`GET /api/game-management/{gameId}/moves-history?skip&take`)
  - DoD:
    - `IGameRepository.GetMovesFilteredByAsync(...)` implemented
    - Handler returns paginated moves based on stored/replayed events
    - Returns deterministic ordering (by event version)
  - Touchpoints: `src/UltimateTicTacToe.Core/Features/GameManagement/GetMovesHistoryQueryHandler.cs`, `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`, `src/UltimateTicTacToe.Core/Projections/ExternalProjections.cs`

- [x] Finish **SignalR group wiring** so clients actually receive game-scoped updates
  - DoD:
    - Client can join a group by `gameId` (string)
    - Server broadcasts `MoveApplied`/`MoveRejected` to that group
  - Touchpoints: `src/UltimateTicTacToe.Api/Hubs/MoveUpdatesHub.cs`, `src/UltimateTicTacToe.Api/RealTimeNotification/MoveUpdatesNotificationHub.cs`

### P1 (Quality + correctness)

- [x] Add controller-level integration tests (real `Program` + routing + DI)
  - DoD:
    - Tests exercise `/api/game/start` and `/api/game/move` end-to-end
    - Uses a test event store (in-memory or Mongo2Go) and asserts persisted events
  - Touchpoints: `tests/UltimateTicTacToe.API.Tests.Unit` (add new integration test project or extend), `src/UltimateTicTacToe.Api/Program.cs`

- [x] Snapshotting: wire `IStateSnapshotStore` into repository and define snapshot triggers
  - DoD:
    - Snapshot created on terminal events / thresholds
    - Load path prefers snapshot + delta events
  - Touchpoints: `src/UltimateTicTacToe.Core/Features/GameSave/StateSnapshotStore.cs`, `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`

- [ ] Improve API contract for moves (return richer payload)
  - DoD:
    - Return current game status + next expected player + last move id/version
    - Use the same payload for HTTP and SignalR events for consistency

### P2 (Observability / ops)

- [x] Add health checks + basic operational endpoints
  - DoD:
    - Health check verifies Mongo connectivity (when enabled)
    - Metrics include unfinished-games count + rooms/queue counters (count-only; no per-endpoint latency yet)
- [ ] Per-endpoint metrics / latency histograms (beyond current count endpoints)

- [x] Trace correlation middleware
  - DoD:
    - Adds/propagates correlation id across logs and responses

## Tech Debt / Risks (Agent Notes)

- [x] `SemaphoreSlim` timeout handling in `InMemoryGameRepository`
  - Note: Ensure `Release()` only happens when `WaitAsync(...)` acquired the semaphore.

- [x] MongoDB event serialization registration is incomplete
  - Note: All domain event types should be registered for polymorphic (de)serialization; keep an integration test to guard this.

- [x] Event replay / rehydration behavior covers key events beyond `CellMarkedEvent`
  - Note: Replay now applies `MiniBoardWonEvent` / `MiniBoardDrawnEvent` deterministically; keep unit tests to guard regressions.

- [ ] Rooms TTL is eventual-consistent (Mongo TTL)
  - Risk: TTL deletion timing is not exact; client UX can be confusing if a room “hangs” until TTL cleans up.
  - Mitigation: keep the expiry sweeper (configurable interval + batch) and treat notifications as best-effort.

## Planned Features

### Persistence

- [x] Implement event sourcing and replay in `IGameRepository`
- [x] Store and retrieve snapshots for `GameRoot`
- [ ] Implement game archive for finished games

### Game Mechanics

- [ ] AI player mode (difficulty levels)
- [ ] Timer-based turn control (timeouts, forfeits)
- [ ] Game validation service (enforce move rules externally)

### API & Monitoring

- [x] RESTful API documentation (OpenAPI + Scalar in Development)
- [x] Metrics endpoints (games now + rooms)
- [x] Health checks (including Mongo connectivity)
- [ ] Rate limiting / throttling (beyond current backpressure)

### Ops / Data seeding

Maintainable **data spawner** so Compose / AWS (or any deployed env) can be seeded without hand-editing Mongo.

- [ ] Versioned seed profiles (e.g. `dev`, `demo`, `load-test`) checked into repo — idempotent, documented, easy to extend
  - DoD: seeds rooms/tickets/metrics fixtures and/or finished-game event streams; safe to re-run (upsert / clear-then-seed flags)
- [ ] Headless runner usable from Compose & cloud
  - DoD: `dotnet run` / container entry (`seed` job or one-shot service) reads Mongo connection from env (`EventStoreSettings` / secrets); exit non-zero on failure; logs what was written
- [ ] Wire into local Compose (optional profile / service) and document AWS/ECS one-shot task equivalent in README
- [ ] Admin seed dashboard (ops UI): enter/confirm Mongo connection (or use app config), pick seed profile, **Seed** / **Clear+Seed** button, show last run result
  - DoD: gated to Development / explicit `Seeding:Enabled` + auth (not exposed on public prod by default); never logs full credentials; works against the same stores the API uses

### Feature toggles & A/B testing

Ship changes behind flags and run controlled experiments without redeploying for every UX/API variant.

- [ ] Feature-toggle service (config + optional Mongo/store) with typed flag keys
  - DoD: boolean / percentage / allow-list targeting; env overlays (Development/Testing/Production); cached reads with short TTL; kill-switch for risky paths (rooms, SignalR payloads, new move contract)
- [ ] `GET /api/features` (or embed in bootstrap) returning effective flags for `X-User-Id` / anonymous
  - DoD: stable assignment for a user across requests; documented in USER_FLOWS + Scalar
- [ ] Gate new backend behaviors behind flags (e.g. richer move payload, rate limiting, archive writes) — default off in prod until validated
- [ ] A/B experiment framework on top of toggles
  - DoD: named experiments with variants (A/B/…), sticky bucketing by user id, traffic split config, exposure event logged (correlation id + experiment + variant)
- [ ] Ops UI or admin endpoints to create/update flags & experiments (auth-gated); audit who changed what
- [ ] Metrics: per-flag evaluation counts; per-experiment conversion hooks (match started, game finished) for later analytics

### Frontend / UI

#### Frontend (Vue3 + Vite + TypeScript + Pinia + custom SCSS)

Lives in sibling repo **`ultimate-tic-tac-toe-front`** (not under this repo’s `frontend/`). Lobby live wiring shipped; Game page still mock (P1).

Contract reference: [docs/USER_FLOWS.md](docs/USER_FLOWS.md) · Front roadmap: sibling `ROADMAP.md`

**Shipped in front**
- [x] Vue3 + Vite + TS + Vue Router + Pinia + Axios + custom SCSS themes
- [x] Routes: Auth, Matchmaking, Game (+ HelloAws)
- [x] Client-side Ultimate TTT rules + matchmaking hub UI
- [x] Dev CORS includes Vite `http://localhost:5173`

**P0 — wire front to this API (lobby)**
- [x] API client live path for Auth + Matchmaking
  - DoD:
    - Sends `X-User-Id` on every request (persisted in local storage)
    - Propagates `X-Correlation-Id` for tracing
    - Normalizes `Result<T>` + HTTP codes (400/403/409/429)
- [x] Lobby + Rooms flow (pre-game)
  - DoD:
    - Lobby: “Play (queue)”, “Create private room”, “Join private room”
    - SignalR `/rooms-hub`: `JoinUser`, handle `QueueJoined`, `MatchFound`, `PrivateRoomCreated`, `QueueExpired`, `RoomExpired`
    - Restore on refresh using `GET /api/rooms/me`
    - Backpressure UX: clear 429 message + retry/backoff
  - Note: `GET /api/rooms/private/{joinCode}` client exists; deeper join-link UX still open

**P1 (Gameplay UI)**
- [ ] Game page `/game/:gameId` live path
  - DoD:
    - History load: `GET /api/game-management/{gameId}/moves-history?skip&take`
    - Realtime: connect `/move-updates-hub`, join game group, apply updates
    - Move submit: `POST /api/game/move` with correct error UX for 400/403/409

**P2 (Shipping & tests)**
- [ ] Dockerized frontend (nginx) + compose
  - DoD: runs in Docker and targets Rider-run API via `http://host.docker.internal:8080`
- [ ] E2E smoke tests (Playwright)
  - DoD: (1) regular matchmaking end-to-end, (2) private create/join end-to-end

- [ ] Multiplayer session management
- [ ] Spectator view

### Analytics

- [ ] Track game durations, player stats
- [ ] Leaderboard and win history
- [ ] Export game data for ML/AI analysis

---

## Testing

- [x] Core integration test project: tests are discovered and runnable
- [x] Storage integration tests (Mongo event store)
- [ ] Property-based testing for game logic
- [ ] Load testing for high concurrency

---

## Nice to Have

- [ ] Game replays and visualizations
- [ ] Tournament brackets (rooms matchmaking already shipped)
- [ ] Internationalization support
- [ ] Mobile-optimized UI

---

## Milestones

| Version | Target Date | Status / Features |
|--------:|-------------|-------------------|
| `v0.1`  | 06.2025     | **Shipped** — MVP with local memory & basic HTTP API |
| `v0.2`  | 07.2025     | **Shipped** — Persistence, replay, rooms/matchmaking, monitoring |
| `v1.0`  | 06.2026     | Public release with live UI + multiplayer (front still mock-first) |

Milestone sequencing suggestion:
- `v1.0`: front live rooms/game HTTP + SignalR → richer move payload → E2E smoke → Docker FE

---

## Notes

- Priorities may shift as project evolves.
- Contributions, ideas, and feedback are welcome!
