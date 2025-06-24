# 🛣️ Ultimate Tic Tac Toe Game Roadmap

This roadmap outlines the planned development and milestones for the Ultimate Tic Tac Toe game project.

---

## ✅ Completed

- [x] Core domain model (`GameRoot`, `Board`, `Cell`)
- [x] In-memory `IGameRepository` with snapshot threshold
- [x] Semaphore-protected concurrent game management
- [x] `GameplayController` with HTTP endpoints
- [x] Result pattern for API responses
- [x] xUnit tests for repository and controller
- [x] Configuration-driven gameplay settings via JSON
- [x] Logging and diagnostics for moves and game lifecycle
- [x] Integration with MongoDB for event store
- [x] Snapshotting mechanism and event persistence
- [x] Event replay to reconstruct game state
- [x] 1. HTTP Endpoint for Sending Moves to Server - Use this to accept new moves from the frontend
- [x] 3. WebSocket Hub for keeping up with Real-Time server's updates
- [x] Integration tests for persistence and controller
- [x] Property-based testing for game logic

---

## 🔄 In Progress

- [ ] HTTP middleware for trace correlation
- [ ] HTTP middleware for CORS security
- [ ] Basic game UI (Vue3) for local play

- [ ] 2. HTTP Endpoint for Initial Move History (With Pagination) - Great for loading full or partial history when the game starts or when reconnecting
---

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

- [ ] RESTful API documentation (Swagger/OpenAPI)
- [ ] Health check & metrics endpoint (GamesNow, memory usage)
- [ ] Rate limiting / throttling

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

---

## 📌 Notes

- Priorities may shift as project evolves.
- Contributions, ideas, and feedback are welcome!