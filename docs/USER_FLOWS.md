# User flows (frontend reference)

This document describes the primary frontend user flows and how they map to backend HTTP + SignalR.

## Conventions
- Identity: `X-User-Id: <guid>` header.
- Scalar (dev): `/scalar/v1`
- Game hub: `/move-updates-hub`
- Rooms hub: `/rooms-hub`

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
  Api-->>Client: 200 OK
  Api-->>HubMove: Broadcast to game group (MoveApplied/MoveRejected)
```

## Reconnect / resume (history + rehydrate)

```mermaid
sequenceDiagram
  participant Client
  participant Api
  participant EventStore
  participant Snapshots

  Client->>Api: GET /api/game-management/{gameId}/moves-history?skip&take
  Api->>EventStore: GetAllEvents(gameId)
  Api-->>Client: 200 { moves[] }

  Note over Api,Snapshots: If game not in memory: load snapshot + delta events, else full replay
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
  Api-->>RoomsHub: MatchFound(A, gameId, opponent=B)
  Api-->>RoomsHub: MatchFound(B, gameId, opponent=A)
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
  Api-->>RoomsHub: MatchFound(Owner, gameId, opponent=Joiner)
  Api-->>RoomsHub: MatchFound(Joiner, gameId, opponent=Owner)
  Api-->>Joiner: 200 { gameId, opponentUserId }
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
  Client->>Api: POST /api/rooms/queue or /api/rooms/private
  Api-->>Client: 429 TooManyRequests (Result<T> with Code=429)
  Note over Client: Client should show "Server busy, retry" and allow retry/backoff
```

