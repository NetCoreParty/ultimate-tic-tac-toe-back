using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UltimateTicTacToe.API.Controllers;
using UltimateTicTacToe.API.Tests.Unit.Extensions;
using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.GamePlay;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.API.Tests.Unit.Controllers;

public class GamePlayControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GamePlayController _sut;

    public GamePlayControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GamePlayController(_mediatorMock.Object);
    }

    [Fact]
    public async Task StartGame_ReturnsOk_WhenGameStartedSuccessfully()
    {
        // Arrange
        var expectedResult = Result<StartGameResponse>.Success(
            new StartGameResponse(
                GameId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                PlayerXId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
                PlayerOId: Guid.Parse("00000000-0000-0000-0000-000000000003"),
                GameState: GameStatus.IN_PROGRESS
                )
            );

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<StartGameCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.StartGame(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<Result<StartGameResponse>>(okResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task StartGame_ReturnsError_WhenFailedToStartGame()
    {
        // Arrange
        var expectedResult = Result<StartGameResponse>.Failure(429, "Please try later. Too many parallel games in memory.");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<StartGameCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.StartGame(CancellationToken.None);

        // Assert
        var failedResult = Assert.IsType<ObjectResult>(result);
        var actualResult = Assert.IsType<Result<StartGameResponse>>(failedResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task MakeMove_ReturnsOk_WhenMoveIsValid()
    {
        // Arrange
        var actualMoveRequest = new PlayerMoveRequest(
            GameId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PlayerId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
            MiniBoardRowId: 0,
            MiniBoardColId: 0,
            CellRowId: 1,
            CellColId: 1
            );

        var expectedResult = Result<bool>.Success(true);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<MakeMoveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.MakeMove(actualMoveRequest, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<Result<bool>>(okResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task MakeMove_ReturnsNotFound_WhenGameNotFound()
    {
        // Arrange
        var gameId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var actualMoveRequest = new PlayerMoveRequest(
            GameId: gameId,
            PlayerId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
            MiniBoardRowId: 0,
            MiniBoardColId: 0,
            CellRowId: 1,
            CellColId: 1
            );

        var expectedResult = Result<bool>.Failure(404, $"Game with ID {gameId} not found.");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<MakeMoveCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.MakeMove(actualMoveRequest, CancellationToken.None);

        // Assert
        var failedResult = Assert.IsType<NotFoundObjectResult>(result);
        var actualResult = Assert.IsType<Result<bool>>(failedResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }
}