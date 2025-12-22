using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Gameplay;

public class InMemoryGameRepositoryPersistenceTests
{
    private static IOptions<GameplaySettings> Settings()
        => Options.Create(new GameplaySettings { MaxActiveGames = 25, EventsUntilSnapshot = 20 });

    [Fact]
    public async Task TryMakeMoveAsync_ShouldRehydrateFromEventStore_WhenGameNotInMemory()
    {
        var store = new InMemoryEventStore();
        var logger = new Mock<ILogger<InMemoryGameRepository>>().Object;
        var snapshots1 = new StateSnapshotStore(Settings());

        var repo1 = new InMemoryGameRepository(store, snapshots1, logger, Settings());
        var start = await repo1.TryStartGameAsync();
        Assert.True(start.IsSuccess);

        // "Restart": new repository instance with empty in-memory dictionary but same event store
        var snapshots2 = new StateSnapshotStore(Settings());
        var repo2 = new InMemoryGameRepository(store, snapshots2, logger, Settings());

        var move = new PlayerMoveRequest(
            GameId: start.Value.GameId,
            PlayerId: start.Value.PlayerXId,
            MiniBoardRowId: 0,
            MiniBoardColId: 0,
            CellRowId: 0,
            CellColId: 0
        );

        var moveResult = await repo2.TryMakeMoveAsync(move);
        Assert.True(moveResult.IsSuccess);

        var events = await store.GetAllEventsAsync(start.Value.GameId);
        Assert.True(events.Count >= 2); // GameCreated + CellMarked (and possibly others)
    }

    [Fact]
    public async Task GetMovesFilteredByAsync_ShouldReturnPaginatedMoves_FromPersistedEvents()
    {
        var store = new InMemoryEventStore();
        var logger = new Mock<ILogger<InMemoryGameRepository>>().Object;
        var snapshots = new StateSnapshotStore(Settings());

        var repo = new InMemoryGameRepository(store, snapshots, logger, Settings());
        var start = await repo.TryStartGameAsync();
        Assert.True(start.IsSuccess);

        // Move 1 (X)
        var m1 = new PlayerMoveRequest(start.Value.GameId, start.Value.PlayerXId, 0, 0, 0, 0);
        Assert.True((await repo.TryMakeMoveAsync(m1)).IsSuccess);

        // Move 2 (O) - next board remains (0,0) because first cell was (0,0)
        var m2 = new PlayerMoveRequest(start.Value.GameId, start.Value.PlayerOId, 0, 0, 0, 1);
        Assert.True((await repo.TryMakeMoveAsync(m2)).IsSuccess);

        var page1 = await repo.GetMovesFilteredByAsync(start.Value.GameId, skip: 0, take: 10, ct: CancellationToken.None);
        Assert.True(page1.IsSuccess);
        Assert.Equal(2, page1.Value.Moves.Count);
        Assert.Equal(1, page1.Value.Moves[0].MoveId);
        Assert.Equal(2, page1.Value.Moves[1].MoveId);

        var page2 = await repo.GetMovesFilteredByAsync(start.Value.GameId, skip: 1, take: 1, ct: CancellationToken.None);
        Assert.True(page2.IsSuccess);
        Assert.Single(page2.Value.Moves);
        Assert.Equal(2, page2.Value.Moves[0].MoveId);
    }
}

