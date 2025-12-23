using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.API.Extensions;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IMatchmakingService _matchmaking;
    private readonly IRoomStore _rooms;
    private readonly IMatchmakingTicketStore _tickets;

    public RoomsController(IMatchmakingService matchmaking, IRoomStore rooms, IMatchmakingTicketStore tickets)
    {
        _matchmaking = matchmaking;
        _rooms = rooms;
        _tickets = tickets;
    }

    [HttpPost("queue")]
    public async Task<IActionResult> Queue(CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var result = await _matchmaking.QueueAsync(userIdResult.Value, ct);
        return result.ToActionResult();
    }

    [HttpDelete("queue/{ticketId:guid}")]
    public async Task<IActionResult> CancelQueue(Guid ticketId, CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var result = await _matchmaking.CancelQueueAsync(userIdResult.Value, ticketId, ct);
        return result.ToActionResult();
    }

    [HttpPost("private")]
    public async Task<IActionResult> CreatePrivate(CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var result = await _matchmaking.CreatePrivateRoomAsync(userIdResult.Value, ct);
        return result.ToActionResult();
    }

    [HttpPost("private/join/{joinCode}")]
    public async Task<IActionResult> JoinPrivate(string joinCode, CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var result = await _matchmaking.JoinPrivateRoomAsync(userIdResult.Value, joinCode, ct);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyRoomState(CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var now = DateTime.UtcNow;
        var ticket = await _tickets.GetActiveTicketForUserAsync(userIdResult.Value, now, ct);
        var room = await _rooms.GetActiveRoomForUserAsync(userIdResult.Value, now, ct);

        return Ok(new
        {
            Ticket = ticket,
            Room = room
        });
    }

    [HttpGet("private/{joinCode}")]
    public async Task<IActionResult> GetPrivateRoomState(string joinCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
            return BadRequest("Join code is required.");

        var now = DateTime.UtcNow;
        var room = await _rooms.GetActivePrivateRoomByJoinCodeAsync(joinCode.Trim(), now, ct);
        if (room == null)
            return NotFound();

        return Ok(room);
    }

    private UltimateTicTacToe.Core.Result<Guid> TryGetUserId()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var values))
            return UltimateTicTacToe.Core.Result<Guid>.Failure(400, "Missing X-User-Id header.");

        var raw = values.FirstOrDefault();
        if (!Guid.TryParse(raw, out var userId))
            return UltimateTicTacToe.Core.Result<Guid>.Failure(400, "Invalid X-User-Id header. Expected GUID.");

        return UltimateTicTacToe.Core.Result<Guid>.Success(userId);
    }
}

