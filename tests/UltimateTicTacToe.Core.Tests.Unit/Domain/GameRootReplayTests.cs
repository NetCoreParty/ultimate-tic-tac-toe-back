using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Domain.Events;

namespace UltimateTicTacToe.Core.Tests.Unit.Domain;

public class GameRootReplayTests
{
    [Fact]
    public void Rehydrate_ShouldApply_MiniBoardWonEvent_Deterministically()
    {
        var gameId = Guid.NewGuid();
        var x = Guid.NewGuid();
        var o = Guid.NewGuid();

        // X wins mini-board (0,0) by filling first row: (0,0), (0,1), (0,2)
        var events = new List<IDomainEvent>
        {
            new GameCreatedEvent(gameId, x, o) { Version = 1 },
            new CellMarkedEvent(gameId, x, 0, 0, 0, 0, PlayerFigure.X) { Version = 2 },
            new CellMarkedEvent(gameId, o, 0, 0, 1, 0, PlayerFigure.O) { Version = 3 },
            new CellMarkedEvent(gameId, x, 0, 0, 0, 1, PlayerFigure.X) { Version = 4 },
            new CellMarkedEvent(gameId, o, 0, 0, 1, 1, PlayerFigure.O) { Version = 5 },
            new CellMarkedEvent(gameId, x, 0, 0, 0, 2, PlayerFigure.X) { Version = 6 },
            new MiniBoardWonEvent(gameId, x, 0, 0, PlayerFigure.X) { Version = 7 },
        };

        var game = GameRoot.Rehydrate(events, null);

        Assert.Equal(GameStatus.IN_PROGRESS, game.Status);
        Assert.Equal(7, game.Version);

        var mini = game.Board.GetMiniBoard(0, 0);
        Assert.Equal(PlayerFigure.X, mini.Winner);
        Assert.True(mini.IsWon);
    }
}

