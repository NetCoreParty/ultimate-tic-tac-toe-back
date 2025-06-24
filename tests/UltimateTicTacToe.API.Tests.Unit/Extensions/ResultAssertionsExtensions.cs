using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.API.Tests.Unit.Extensions;

public static class ResultAssertionsExtensions
{
    public static void ShouldBeEquivalentTo<T>(this Result<T> actual, Result<T> expected)
    {
        Assert.Equal(expected.Value, actual.Value);
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.IsSuccess, actual.IsSuccess);
        Assert.Equal(expected.Error, actual.Error);
    }
}