using MediatR;
using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.API.Extensions;
using UltimateTicTacToe.Core.Features.Metrics;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRoomStore _rooms;
    private readonly IMatchmakingTicketStore _tickets;
    private readonly IRoomMetricsStore _roomMetrics;

    public MetricsController(IMediator mediator, IRoomStore rooms, IMatchmakingTicketStore tickets, IRoomMetricsStore roomMetrics)
    {
        _mediator = mediator;
        _rooms = rooms;
        _tickets = tickets;
        _roomMetrics = roomMetrics;
    }

    [HttpGet("unfinished-games")]
    public async Task<IActionResult> GetUnfinishedGames(CancellationToken ct = default)
    {
        var gamesUnfinishedResult = await _mediator.Send(new GetUnfinishedGamesQuery(), ct);
        return gamesUnfinishedResult.ToActionResult();
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRoomsMetrics(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var activeRegular = await _rooms.CountActiveRoomsAsync(RoomType.Regular, ct);
        var activePrivate = await _rooms.CountActiveRoomsAsync(RoomType.Private, ct);
        var queued = await _tickets.CountQueuedTicketsAsync(now, ct);
        var created = await _roomMetrics.GetRoomsCreatedCountersAsync(ct);

        return Ok(new
        {
            ActiveRegularRooms = activeRegular,
            ActivePrivateRooms = activePrivate,
            QueuedTickets = queued,
            RoomsCreated = new { Regular = created.RegularCreated, Private = created.PrivateCreated }
        });
    }
}