# 🧠 Ultimate Tic Tac Toe

A domain-driven, event-sourced implementation of Ultimate Tic Tac Toe with a clean separation of concerns between API, application, and domain layers.
Built using .NET 7, this project serves both as a game and as a reference architecture for modern DDD/CQRS patterns.

---

## 🎯 Project Goals

- Implement Ultimate Tic Tac Toe with rule-enforced gameplay
- Leverage Event Sourcing for state reconstruction
- Expose a clean HTTP API for starting games and making moves
- Keep the system scalable, testable, and observable

---

## 🧱 Tech Stack

- **.NET 7**
- **ASP.NET Core Web API**
- **xUnit** for testing
- **MongoDB** (planned) for Event Store
- **Result<T> Pattern** for clean API response mapping

---

## 🚀 Features

- Start new games with player IDs
- Make legal moves on a 9x9 board
- Automatically determine game outcomes (Win / Draw)
- In-memory game repository with configurable limits
- Domain-first architecture with game state logic encapsulated in `GameRoot`
- Background cleanup of finished games
- Monitoring endpoint for active games

---

## 🧩 Custom Logic / Conventions

- **Event sourcing + replay (restart-safe)**:
  - Game creation and moves are persisted as domain events to Mongo via `IEventStore`.
  - If a game isn’t found in memory, it is rehydrated from stored events (event replay) and play continues.
  - Key code: `src/UltimateTicTacToe.Core/Services/InMemoryGameRepository.cs`, `src/UltimateTicTacToe.Storage/Services/MongoEventStore.cs`

- **Moves history is derived from events**:
  - `/api/game-management/{gameId}/moves-history?skip&take` reads persisted `CellMarkedEvent`s ordered by event version.
  - Key code: `src/UltimateTicTacToe.Core/Features/GameManagement/GetMovesHistoryQueryHandler.cs`

- **Capacity backpressure (near-capacity throttling)**:
  - Hard cap: `GameplaySettings.MaxActiveGames`
  - Backpressure starts at:

    \( threshold = \lceil MaxActiveGames \cdot BackpressureThresholdPercent / 100 \rceil \)
  - When `activeGames >= threshold`, the server returns **HTTP 429** for new admissions.
    (This is implemented for game start now; the same rule will be applied to rooms queue + private create/join.)
  - Dev defaults: `MaxActiveGames=140`, `BackpressureThresholdPercent=90` ⇒ threshold = **126**.
  - Config: `src/UltimateTicTacToe.Api/appsettings.Development.json`
---

## 🔧 Getting Started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [MongoDB](https://www.mongodb.com/try/download/community) for persistence

### Run the API

```bash
dotnet run --project GameApi
```

### HTTP Endpoints

```    
    Method		URL			                Purpose
    
    POST		api/game/start		        Start a new game
    POST		api/game/move   		    Make a move in the game        
    DELETE		api/game/clear		        Delete all the finished games (WON or DRAW) from memory
    GET		    api/game/metrics/games-now	Get all in-memory games count
```

### 🧪 Testing

```bash
dotnet test
```

### 📁 Project Structure

```

    /GameApi
      Controllers/
      Configuration/
      Program.cs
    
    /GameDomain
      GameRoot.cs
      GameStatus.cs
      Move validation logic
    
    /GameInfrastructure
      InMemoryGameRepository.cs
      MongoEventStore.cs (planned)
    
    /GameTests
      InMemoryGameRepositoryTests.cs
      GameplayControllerTests.cs

```

### 🤝 Contributions

Feel free to fork, file issues, or submit PRs. Feedback and feature requests are welcome!

### 📄 License

This project is licensed under the MIT License.