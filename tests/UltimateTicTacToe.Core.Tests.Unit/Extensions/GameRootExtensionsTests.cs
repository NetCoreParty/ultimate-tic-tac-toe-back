using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities;
using UltimateTicTacToe.Core.Extensions;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities.Snapshot;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;

namespace UltimateTicTacToe.Core.Tests.Unit.Extensions;

public class GameRootExtensionsTests
{
    [Fact]
    public void ToSnapshot_Should_MapAllFields_Correctly()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerX = Guid.NewGuid();
        var playerO = Guid.NewGuid();

        var game = GameRoot.CreateNew(gameId, playerX, playerO);

        // Simulate a few moves
        game.PlayMove(game.PlayerXId, 0, 0, 0, 0);
        game.PlayMove(game.PlayerOId, 0, 0, 0, 1);

        // Act
        var snapshot = game.ToSnapshot();

        // Assert
        Assert.Equal(game.GameId, snapshot.GameId);
        Assert.Equal(game.PlayerXId, snapshot.PlayerXId);
        Assert.Equal(game.PlayerOId, snapshot.PlayerOId);
        Assert.Equal((int)game.Status, snapshot.Status);
        Assert.Equal(game.WinnerId, snapshot.WinnerId);
        Assert.Equal(game.Version, snapshot.Version);
        Assert.Equal(1, snapshot.MiniBoards.Count); // Players filled only two cells in one mini board, so only one mini board should be present

        var affectedMiniBoard = snapshot.MiniBoards.First(b => b.Row == 0 && b.Col == 0);
        Assert.Equal(2, affectedMiniBoard.Cells.Count);
        Assert.Equal(PlayerFigure.X.ToString(), affectedMiniBoard.Cells.First(c => c.Row == 0 && c.Col == 0).Figure);
        Assert.Equal(PlayerFigure.O.ToString(), affectedMiniBoard.Cells.First(c => c.Row == 0 && c.Col == 1).Figure);
    }

    [Fact(Skip = "ToGameRoot() method cant be used, because we dont know exactly the order of moves to recreate a snapshot from a certain point, think about it later")]
    public void ToGameRoot_Should_ReconstructGame_Correctly()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerX = Guid.NewGuid();
        var playerO = Guid.NewGuid();

        var snapshot = new GameRootSnapshotProjection
        {
            GameId = gameId,
            PlayerXId = playerX,
            PlayerOId = playerO,
            Status = (int)GameStatus.IN_PROGRESS,
            WinnerId = null,
            Version = 3,
            MiniBoards = new List<MiniBoardSnapshot>()
        };

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                snapshot.MiniBoards.Add(new MiniBoardSnapshot
                {
                    Row = r,
                    Col = c,
                    Winner = null,
                    Cells = new List<CellSnapshot>()
                });

                for (int cr = 0; cr < 3; cr++)
                {
                    for (int cc = 0; cc < 3; cc++)
                    {
                        snapshot.MiniBoards.Last().Cells.Add(new CellSnapshot
                        {
                            Row = cr,
                            Col = cc,
                            Figure = null
                        });
                    }
                }
            }
        }

        var events = new List<IDomainEvent>();

        // Act
        var gameRoot = snapshot.ToGameRoot_DoesntWorkForNow(events);

        // Assert
        Assert.Equal(gameId, gameRoot.GameId);
        Assert.Equal(playerX, gameRoot.PlayerXId);
        Assert.Equal(playerO, gameRoot.PlayerOId);
    }
}