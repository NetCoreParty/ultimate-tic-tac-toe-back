# User flows (frontend reference)

This document describes the primary frontend user flows and how they map to backend HTTP + SignalR.

## Conventions
- Identity: `X-User-Id: <guid>` header.
- Scalar (dev): `/scalar/v1`
- Game hub: `/move-updates-hub`
- Rooms hub: `/rooms-hub`
- `MatchFound` payload: `{ ticketId, gameId, opponentUserId, playerXId, playerOId }` (player ids = user GUIDs; X is room `Players[0]`)
- **Host** = `playerXId` (room `Players[0]` / first queue seat / private room creator). Guest = `playerOId`.

## Leave game (host forfeit / guest soft-leave)

```mermaid
sequenceDiagram
  participant Host as Host_X
  participant Guest as Guest_O
  participant Api
  participant MoveHub

  Guest->>Api: POST /api/game/leave (X-User-Id=O)
  Api-->>Guest: 200 { outcome: softLeave }
  Api-->>MoveHub: OpponentDisconnected
  MoveHub-->>Host: status Opponent disconnected
  Guest->>MoveHub: JoinGame again (share link)
  MoveHub-->>Host: OpponentReconnected

  Host->>Api: POST /api/game/leave (X-User-Id=X)
  Api->>Api: FullGameWonEvent(winner=O)
  Api-->>Host: 200 { outcome: forfeit, winnerId }
  Api-->>MoveHub: GameEnded reason=forfeit
  MoveHub-->>Guest: winner modal
```

- Hub events: `GameEnded`, `OpponentDisconnected`, `OpponentReconnected` (plus existing `MoveApplied` / `MoveRejected`).
- Moves history also returns `status` + `winnerId` so forfeit survives refresh.
- Front UX: guest soft-leave **keeps** `sessionStorage` game session (share-link rejoin); host forfeit **clears** it. Off-turn / illegal clicks use a fixed toast (no layout jump). Theme is chosen on Auth/Matchmaking only (locked in-game).

## Ops probes
- Deploy / ALB liveness: `GET /health/live` (JSON `{ status, checks }`, no descriptions).
- Readiness (Mongo when `HealthChecks:MongoEnabled`): `GET /health/ready`.

## Start game and play moves

```mermaid
sequenceDiagram
  participant Client
  participant Api
  participant EventStore
  participant HubMove

  Client->>Api: POST /api/game/start
  Api->>EventStore: AppendEvents(GameCreated)
  Api-->>Client: 200 { gameId, playerXId, playerOId }

  Client->>HubMove: Connect + JoinGame(gameId)

  Client->>Api: POST /api/game/move (PlayerMoveRequest)
  Api->>EventStore: AppendEvents(CellMarked + maybe terminal events)
  Api-->>Client: 200 MoveResult { gameId, status, winnerId, version, nextPlayerId, lastMove }
  Api-->>HubMove: MoveApplied(MoveResult) / MoveRejected(string)
```

`MoveResult` is shared by HTTP and SignalR so peers apply the last move locally (no full history refetch per move). Clients still load history on enter/reconnect.

## Reconnect / resume (history + rehydrate)

```mermaid
sequenceDiagram
  participant Client
  participant Api
  participant EventStore
  participant Snapshots

  Client->>Api: GET /api/game-management/{gameId}/moves-history?skip&take
  Api->>EventStore: GetAllEvents(gameId)
  Api-->>Client: 200 { moves[], status, winnerId }

  Note over Api,Snapshots: If game not in memory: load snapshot + delta events, else full replay
  Note over Client: status/winnerId cover forfeit wins with no board meta-winner
```

## Regular matchmaking (queue -> match -> game)

```mermaid
sequenceDiagram
  participant ClientA
  participant ClientB
  participant Api
  participant RoomsHub
  participant EventStore

  ClientA->>RoomsHub: Connect + JoinUser (X-User-Id=A)
  ClientB->>RoomsHub: Connect + JoinUser (X-User-Id=B)

  ClientA->>Api: POST /api/rooms/queue (X-User-Id=A)
  Api-->>ClientA: 200 { ticketId }
  Api-->>RoomsHub: QueueJoined(A)

  ClientB->>Api: POST /api/rooms/queue (X-User-Id=B)
  Api->>EventStore: AppendEvents(GameCreated for A+B)
  Api-->>RoomsHub: MatchFound(A, gameId, opponent=B, playerXId, playerOId)
  Api-->>RoomsHub: MatchFound(B, gameId, opponent=A, playerXId, playerOId)
  Api-->>ClientB: 200 { ticketId }
```

## Private room (create -> share code -> join -> game)

```mermaid
sequenceDiagram
  participant Owner
  participant Joiner
  participant Api
  participant RoomsHub
  participant EventStore

  Owner->>RoomsHub: Connect + JoinUser (X-User-Id=Owner)
  Joiner->>RoomsHub: Connect + JoinUser (X-User-Id=Joiner)

  Owner->>Api: POST /api/rooms/private
  Api-->>Owner: 200 { joinCode }
  Api-->>RoomsHub: PrivateRoomCreated(Owner, joinCode)

  Note over Owner,Joiner: Owner shares joinCode/link out-of-band

  Joiner->>Api: POST /api/rooms/private/join/{joinCode}
  Api->>EventStore: AppendEvents(GameCreated)
  Api-->>RoomsHub: MatchFound(Owner, gameId, opponent=Joiner, playerXId, playerOId)
  Api-->>RoomsHub: MatchFound(Joiner, gameId, opponent=Owner, playerXId, playerOId)
  Api-->>Joiner: 200 { gameId, playerXId, playerOId, opponentUserId }
```

## Expiry flows (best-effort notifications)

```mermaid
sequenceDiagram
  participant Worker
  participant Api
  participant RoomsHub
  participant Mongo

  Note over Mongo: TTL deletes are eventual; worker sweeps frequently
  Worker->>Mongo: Find expired queued tickets / half-full rooms
  Worker-->>RoomsHub: QueueExpired(userId, ticketId)
  Worker-->>RoomsHub: RoomExpired(userId, roomId, type)
```

## Backpressure (near capacity -> 429)

```mermaid
sequenceDiagram
  participant Client
  participant Api

  Note over Api: threshold = ceil(MaxActiveGames * BackpressureThresholdPercent / 100)
  Client->>Api: POST /api/rooms/queue or /api/rooms/private or /api/rooms/private/join/{joinCode} (also POST /api/game/start)
  Api-->>Client: 429 TooManyRequests (Result<T> with Code=429)
  Note over Client: Client should show "Server busy, retry" and allow retry/backoff
```

