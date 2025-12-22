# 🛣️ Ultimate Tic Tac Toe Game Roadmap

This roadmap outlines the planned development and milestones for the Ultimate Tic Tac Toe game project.

---

## ✅ Completed

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
- [x] 1. HTTP Endpoint for Sending Moves to Server - Use this to accept new moves from the frontend
- [x] 3. WebSocket Hub for keeping up with Real-Time server's updates
- [ ] Snapshotting mechanism wired end-to-end (repo uses snapshots + clears uncommitted changes)
- [x] Event persistence + replay wired end-to-end (repo appends events, rehydrates on demand)
- [ ] Integration tests for controller/API surface
- [ ] Property-based testing for game logic
- [x] Rooms + matchmaking (Regular/Private) + TTL + metrics + expiry notifications
- [x] Capacity backpressure (429) for new admissions near `MaxActiveGames` + documented in README

---

## 🔄 In Progress

- [ ] HTTP middleware for trace correlation
- [ ] HTTP middleware for CORS security
- [ ] Basic game UI (Vue3) for local play

- [x] 2. HTTP Endpoint for Initial Move History (With Pagination) - Great for loading full or partial history when the game starts or when reconnecting
---

## 💡 Suggested Next Steps (Agent Review)

These are my recommendations based on the current repo state (API/Core/Storage/tests) and what’s most likely to unblock the next features.

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

- [ ] Add controller-level integration tests (real `Program` + routing + DI)
  - DoD:
    - Tests exercise `/api/game/start` and `/api/game/move` end-to-end
    - Uses a test event store (in-memory or Mongo2Go) and asserts persisted events
  - Touchpoints: `tests/UltimateTicTacToe.API.Tests.Unit` (add new integration test project or extend), `src/UltimateTicTacToe.Api/Program.cs`

- [ ] Snapshotting: wire `IStateSnapshotStore` into repository and define snapshot triggers
  - DoD:
    - Snapshot created on terminal events / thresholds
    - Load path prefers snapshot + delta events
  - Touchpoints: `src/UltimateTicTacToe.Core/Features/GameSave/StateSnapshotStore.cs`, `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`

- [ ] Improve API contract for moves (return richer payload)
  - DoD:
    - Return current game status + next expected player + last move id/version
    - Use the same payload for HTTP and SignalR events for consistency

### P2 (Observability / ops)

- [ ] Add health checks + basic operational endpoints
  - DoD:
    - Health check verifies Mongo connectivity (when enabled)
    - Metrics include active games, per-endpoint counts/latency (minimal)

- [ ] Trace correlation middleware
  - DoD:
    - Adds/propagates correlation id across logs and responses

## ⚠️ Tech Debt / Risks (Agent Notes)

- [x] `SemaphoreSlim` timeout handling in `InMemoryGameRepository`
  - Note: Ensure `Release()` only happens when `WaitAsync(...)` acquired the semaphore.

- [x] MongoDB event serialization registration is incomplete
  - Note: All domain event types should be registered for polymorphic (de)serialization; keep an integration test to guard this.

- [ ] Event replay / rehydration behavior is partially implemented
  - Risk: `GameRoot.PlayMove(..., isEventReplay:true)` returns early, and replay currently relies on `When(CellMarkedEvent)` mutating the board. This can diverge as more event types are introduced (mini-board wins, full-game wins, draws).
  - Fix direction: define a consistent “apply” path for each event type during replay, so rehydration is correct and future-proof.

- [ ] Rooms TTL is eventual-consistent (Mongo TTL)
  - Risk: TTL deletion timing is not exact; client UX can be confusing if a room “hangs” until TTL cleans up.
  - Mitigation: keep the expiry sweeper (configurable interval + batch) and treat notifications as best-effort.

## 📝 Planned Features

### 📦 Persistence

- [ ] Implement event sourcing and replay in `IGameRepository`
- [ ] Store and retrieve snapshots for `GameRoot`
- [ ] Implement game archive for finished games

### 🧠 Game Mechanics

- [ ] AI player mode (difficulty levels)
- [ ] Timer-based turn control (timeouts, forfeits)
- [ ] Game validation service (enforce move rules externally)

### 🌐 API & Monitoring

- [x] RESTful API documentation (OpenAPI + Scalar in Development)
- [x] Metrics endpoints (games now + rooms)
- [ ] Health checks (including Mongo connectivity)
- [ ] Rate limiting / throttling (beyond current backpressure)

### 🎮 Frontend / UI

- [ ] Basic game UI (Vue3)
- [ ] Multiplayer session management
- [ ] Spectator view

### 📊 Analytics

- [ ] Track game durations, player stats
- [ ] Leaderboard and win history
- [ ] Export game data for ML/AI analysis

---

## 🧪 Testing

- [x] Core integration test project: tests are discovered and runnable
- [x] Storage integration tests (Mongo event store)
- [ ] Load testing for high concurrency

---

## 🧩 Nice to Have

- [ ] Game replays and visualizations
- [ ] Tournament / matchmaking system
- [ ] Internationalization support
- [ ] Mobile-optimized UI

---

## 📅 Milestones

| Version | Target Date | Features									|
|--------:|-------------|------------------------------------------	|
| `v0.1`  | 06.2025     | MVP with local memory & basic HTTP API	|
| `v0.2`  | 07.2025     | Persistence layer, replay, and monitoring |
| `v1.0`  | 10.2025     | Public release with UI and multiplayer	|

Milestone sequencing suggestion:
- `v0.2`: persistence (append + replay) → moves history endpoint → reconnect flow (snapshot optional but recommended)

---

## 📌 Notes

- Priorities may shift as project evolves.
- Contributions, ideas, and feedback are welcome!