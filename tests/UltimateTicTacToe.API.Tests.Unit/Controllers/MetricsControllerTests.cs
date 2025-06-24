using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UltimateTicTacToe.API.Controllers;
using UltimateTicTacToe.API.Tests.Unit.Extensions;
using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Features.Metrics;

namespace UltimateTicTacToe.API.Tests.Unit.Controllers;

public class MetricsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly MetricsController _sut;

    public MetricsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new MetricsController(_mediatorMock.Object);
    }    

    [Fact]
    public async Task GetGamesNow_Returns_SuccessfulResult()
    {
        // Arrange
        var expectedResult = Result<int>.Success(5);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUnfinishedGamesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        // Act
        var result = await _sut.GetUnfinishedGames(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResult = Assert.IsType<Result<int>>(okResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetGamesNow_Returns_FailedResult()
    {
        // Arrange
        var expectedResult = Result<int>.Failure(400, "Something 400-ish happened");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUnfinishedGamesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetUnfinishedGames(CancellationToken.None);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var actualResult = Assert.IsType<Result<int>>(badResult.Value);
        actualResult.ShouldBeEquivalentTo(expectedResult);
    }
}