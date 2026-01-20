## UltimateTicTacToe snapshot calculation

Each player move might generate any of these events:

Action: Player makes move -> Event: CellMarkedEvent
Action: MiniBoard won -> Event: (maybe) MiniBoardWonEvent
Action: Game won -> Event: (maybe) GameWonEvent

---

9 mini boards (3x3)
9 cells per mini board
So, 81 total possible moves maximum (~100 events per full game (max))

---

Strategy: Take a snapshot every 15–25 moves
Strategies comparison:
Every 10 moves Very fast rehydration Slightly more storage
Every 20 moves Balanced Small snapshot size, fast replay
Only at game end Lowest storage Slower real-time rehydration

---

Better solution - take a snapshot if:
✅ A mini board is won
✅ 20 moves since last snapshot
✅ Game ends (final snapshot)
❗If AI analysis is about to run, snapshot first

That gives you:
4–5 snapshots per game (at most)
Less than 10 events to replay on average
Great performance for real-time AI or replay

---

public record GameSnapshot
{
public Guid GameId { get; init; }
public int Version { get; init; }
public string SerializedState { get; init; } // JSON/Binary
}

if (eventCountSinceLastSnapshot >= 20 || evt is MiniBoardWonEvent || evt is GameWonEvent)
{
var snapshot = new GameSnapshot
{
GameId = game.Id,
Version = currentEventVersion,
SerializedState = JsonConvert.SerializeObject(game)
};

    snapshotStore.Save(snapshot);
    eventCountSinceLastSnapshot = 0;

## }

In a Domain-Driven Design (DDD) setup, especially with event sourcing, the root aggregate (in your case probably something like GameRoot or UltimateGame) should be the one that:
1_Owns the full state of the game (including the UltimateBoard)
2_Applies commands like ApplyMove
3_Tracks and emits domain events
4_Holds invariants and versioning
5_Exposes DequeueUncommittedEvents()
This ensures that your domain logic is encapsulated, and only the root can mutate internal state consistently

---

High-Level Logic of keeping game state actual, and what UncommittedChanges for:

var game = eventStore.Load<GameRoot>(gameId);
game.MakeMove(...);
var events = game.UncommittedChanges;
eventStore.Save(gameId, events);

## var game = eventStore.Load<GameRoot>(gameId); // Uses all stored events

Shapshot flow:

// 1. Load aggregate (rehydrated from snapshot + remaining events)
var game = eventStore.Load<GameRoot>(gameId);

// 2. Apply command, which produces new uncommitted events
game.MakeMove(...);

// 3. Save uncommitted events
var newEvents = game.UncommittedChanges;
eventStore.Save(gameId, newEvents, expectedVersion: game.Version);

// 4. Check if it's time to create a snapshot (e.g., every N events)
if (game.Version % 20 == 0) {

    var snapshot = new GameSnapshot {
        Id = game.Id,
        Version = game.Version,
        State = JsonConvert.SerializeObject(game.ExportState())
    };

    snapshotStore.Save(snapshot);

}

// 5. Clear uncommitted changes
game.MarkChangesAsCommitted();

---

Think of it like this:

GameRoot = rehydrated domain object
EventStore = append-only log
SnapshotStore = read-optimized shortcut
UncommittedChanges = temporary buffer before persistence
This separation ensures clean, predictable behavior

---
