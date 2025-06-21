using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UltimateTicTacToe.API.Controllers;
using UltimateTicTacToe.API.Tests.Unit.Extensions;
using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Features.GameManagement;

namespace UltimateTicTacToe.API.Tests.Unit.Controllers;

public class GameManagementControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GameManagementController _sut;

    public GameManagementControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GameManagementController(_mediatorMock.Object);
    }

    [Fact]
    public async Task ClearFinishedGames_Returns_SuccessfulResult()
    {
        // Arrange
        var expectedResult = Result<bool>.Success(true);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ClearFinishedGamesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.ClearFinishedGames(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<Result<bool>>(okResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task ClearFinishedGames_Returns_FailedResult()
    {
        // Arrange
        var expectedResult = Result<bool>.Failure(500, "Smth bad happened on server");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ClearFinishedGamesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.ClearFinishedGames(CancellationToken.None);

        // Assert
        var badResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, badResult.StatusCode);
    }
}