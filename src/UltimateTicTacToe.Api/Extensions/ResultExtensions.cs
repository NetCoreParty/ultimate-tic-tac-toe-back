using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.Core;

namespace UltimateTicTacToe.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result)
            {
                StatusCode = result.Code
            };
        }

        return result.Code switch
        {
            400 => new BadRequestObjectResult(result),
            401 => new UnauthorizedObjectResult(result),
            403 => new ObjectResult(result) { StatusCode = 403 },
            404 => new NotFoundObjectResult(result),
            500 => new ObjectResult(new { error = "Internal server error. Server failed during processing this request. See logs to find out more..." }) { StatusCode = 500 },
            _ => new ObjectResult(result)
        };
    }
}