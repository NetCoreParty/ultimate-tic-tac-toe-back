using Microsoft.AspNetCore.Mvc;
using Moq;
using UltimateTicTacToe.API.Controllers;
using UltimateTicTacToe.Core.Features.Gameplay;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UltimateTicTacToe.API.Tests.Unit.Controllers;

public class GameplayControllerTests
{
    private readonly Mock<IGameRepository> _repositoryMock;
    private readonly GameplayController _controller;

    public GameplayControllerTests()
    {
        _repositoryMock = new Mock<IGameRepository>();
        _controller = new GameplayController(_repositoryMock.Object);
    }

    [Fact]
    public async Task StartGame_ReturnsOk_WhenGameStartedSuccessfully()
    {
        var response = new StartGameResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _repositoryMock.Setup(x => x.TryStartGameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StartGameResponse>.Success(response));

        var result = await _controller.StartGame(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task StartGame_ReturnsError_WhenFailedToStartGame()
    {
        _repositoryMock.Setup(x => x.TryStartGameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StartGameResponse>.Failure(429, "Too many games"));

        var result = await _controller.StartGame(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, objectResult.StatusCode);
        Assert.Equivalent(new { error = "Too many games" }, objectResult.Value);
    }

    [Fact]
    public async Task MakeMove_ReturnsOk_WhenMoveIsValid()
    {
        var move = new PlayerMoveRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 1, 1);
        _repositoryMock.Setup(x => x.TryMakeMoveAsync(move, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.MakeMove(move, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, okResult.Value);
    }

    [Fact]
    public async Task MakeMove_ReturnsNotFound_WhenGameNotFound()
    {
        var move = new PlayerMoveRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 1, 1);
        _repositoryMock.Setup(x => x.TryMakeMoveAsync(move, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(404, "Game not found"));

        var result = await _controller.MakeMove(move, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        Assert.Equivalent(new { error = "Game not found" }, objectResult.Value);
    }

    [Fact]
    public async Task ClearFinishedGames_ReturnsOk()
    {
        _repositoryMock.Setup(x => x.TryClearFinishedGames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ClearFinishedGames(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True((bool)okResult.Value);
    }

    [Fact]
    public void GetGamesNow_ReturnsCorrectCount()
    {
        _repositoryMock.Setup(x => x.GamesNow).Returns(5);

        var result = _controller.GetGamesNow();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equivalent(new { GamesNow = 5 }, okResult.Value);
    }
}