using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.Rooms;

public interface IMatchmakingService
{
    Task<Result<QueueForGameResponse>> QueueAsync(Guid userId, CancellationToken ct);
    Task<Result<bool>> CancelQueueAsync(Guid userId, Guid ticketId, CancellationToken ct);

    Task<Result<CreatePrivateRoomResponse>> CreatePrivateRoomAsync(Guid userId, CancellationToken ct);
    Task<Result<JoinPrivateRoomResponse>> JoinPrivateRoomAsync(Guid userId, string joinCode, CancellationToken ct);
}

public class MatchmakingService : IMatchmakingService
{
    private readonly IRoomStore _rooms;
    private readonly IMatchmakingTicketStore _tickets;
    private readonly IRoomMetricsStore _metrics;
    private readonly IGameRepository _games;
    private readonly IRoomsNotifier _notifier;
    private readonly RoomSettings _roomSettings;
    private readonly GameplaySettings _gameplaySettings;
    private readonly ILogger<MatchmakingService> _logger;

    public MatchmakingService(
        IRoomStore rooms,
        IMatchmakingTicketStore tickets,
        IRoomMetricsStore metrics,
        IGameRepository games,
        IRoomsNotifier notifier,
        IOptions<RoomSettings> roomSettings,
        IOptions<GameplaySettings> gameplaySettings,
        ILogger<MatchmakingService> logger)
    {
        _rooms = rooms;
        _tickets = tickets;
        _metrics = metrics;
        _games = games;
        _notifier = notifier;
        _roomSettings = roomSettings.Value;
        _gameplaySettings = gameplaySettings.Value;
        _logger = logger;
    }

    public async Task<Result<QueueForGameResponse>> QueueAsync(Guid userId, CancellationToken ct)
    {
        if (IsInBackpressure())
            return Result<QueueForGameResponse>.Failure(429, "Server is near capacity. Please retry shortly.");

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_roomSettings.RoomTtlMinutes);

        // Enforce regular rooms cap (count only active waiting rooms).
        var activeRegularRooms = await _rooms.CountActiveRoomsAsync(RoomType.Regular, ct);
        if (activeRegularRooms >= _roomSettings.MaxRegularRooms)
            return Result<QueueForGameResponse>.Failure(429, "No regular rooms available. Please retry shortly.");

        var ticket = await _tickets.CreateQueuedTicketAsync(userId, now, expiresAt, ct);
        await _notifier.NotifyQueueJoinedAsync(userId, new QueueForGameResponse(ticket.TicketId, ticket.ExpiresAtUtc), ct);

        // Try to match into existing waiting room; otherwise create a new waiting room.
        var matchedRoom = await _rooms.TryJoinWaitingRegularRoomAsync(userId, now, expiresAt, ct);
        if (matchedRoom == null)
        {
            await _rooms.CreateWaitingRegularRoomAsync(userId, now, expiresAt, ct);
            await _metrics.IncrementRoomsCreatedAsync(RoomType.Regular, ct);
            return Result<QueueForGameResponse>.Success(new QueueForGameResponse(ticket.TicketId, expiresAt));
        }

        // Matched -> create game and notify both users, then delete the room (room exists only pre-game).
        var p1 = matchedRoom.Players[0].UserId; // first player = X
        var p2 = matchedRoom.Players[1].UserId; // second player = O

        var start = await _games.TryStartGameForPlayersAsync(p1, p2, gameId: null, ct);
        if (!start.IsSuccess)
        {
            _logger.LogWarning($"{nameof(MatchmakingService)}:{nameof(QueueAsync)}(): Failed to create game after match: {start.Code} {start.Error}");
            await _rooms.DeleteRoomAsync(matchedRoom.RoomId, ct);
            return Result<QueueForGameResponse>.Failure(start.Code, start.Error ?? "Failed to create game.");
        }

        await _tickets.TryMarkMatchedAsync(ticket.TicketId, matchedRoom.RoomId, start.Value!.GameId, ct);

        // Best-effort: notify both users.
        await _notifier.NotifyMatchFoundAsync(p1, new MatchFoundNotification(ticket.TicketId, start.Value!.GameId, p2), ct);
        await _notifier.NotifyMatchFoundAsync(p2, new MatchFoundNotification(ticket.TicketId, start.Value!.GameId, p1), ct);

        await _rooms.DeleteRoomAsync(matchedRoom.RoomId, ct);

        return Result<QueueForGameResponse>.Success(new QueueForGameResponse(ticket.TicketId, expiresAt));
    }

    public async Task<Result<bool>> CancelQueueAsync(Guid userId, Guid ticketId, CancellationToken ct)
    {
        var ok = await _tickets.TryCancelAsync(ticketId, userId, ct);
        return ok ? Result<bool>.Success(true) : Result<bool>.Failure(404, "Ticket not found or not cancellable.");
    }

    public async Task<Result<CreatePrivateRoomResponse>> CreatePrivateRoomAsync(Guid userId, CancellationToken ct)
    {
        if (IsInBackpressure())
            return Result<CreatePrivateRoomResponse>.Failure(429, "Server is near capacity. Please retry shortly.");

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_roomSettings.RoomTtlMinutes);

        var activePrivateRooms = await _rooms.CountActiveRoomsAsync(RoomType.Private, ct);
        if (activePrivateRooms >= _roomSettings.MaxPrivateRooms)
            return Result<CreatePrivateRoomResponse>.Failure(429, "No private rooms available. Please retry shortly.");

        // Generate a short join code; store enforces uniqueness.
        var joinCode = JoinCodeGenerator.Generate(10);
        var room = await _rooms.CreatePrivateRoomAsync(userId, joinCode, now, expiresAt, ct);
        await _metrics.IncrementRoomsCreatedAsync(RoomType.Private, ct);

        var payload = new CreatePrivateRoomResponse(room.RoomId, room.JoinCode!, room.ExpiresAtUtc);
        await _notifier.NotifyPrivateRoomCreatedAsync(userId, payload, ct);
        return Result<CreatePrivateRoomResponse>.Success(payload);
    }

    public async Task<Result<JoinPrivateRoomResponse>> JoinPrivateRoomAsync(Guid userId, string joinCode, CancellationToken ct)
    {
        if (IsInBackpressure())
            return Result<JoinPrivateRoomResponse>.Failure(429, "Server is near capacity. Please retry shortly.");

        if (string.IsNullOrWhiteSpace(joinCode))
            return Result<JoinPrivateRoomResponse>.Failure(400, "Join code is required.");

        var now = DateTime.UtcNow;
        var room = await _rooms.TryJoinPrivateRoomAsync(userId, joinCode.Trim(), now, ct);
        if (room == null)
            return Result<JoinPrivateRoomResponse>.Failure(404, "Room not found or not joinable.");

        if (room.Players.Count != 2)
            return Result<JoinPrivateRoomResponse>.Failure(409, "Room is not full yet.");

        var p1 = room.Players[0].UserId;
        var p2 = room.Players[1].UserId;
        var start = await _games.TryStartGameForPlayersAsync(p1, p2, gameId: null, ct);
        if (!start.IsSuccess)
        {
            _logger.LogWarning($"{nameof(MatchmakingService)}:{nameof(JoinPrivateRoomAsync)}(): Failed to create game after private join: {start.Code} {start.Error}");
            await _rooms.DeleteRoomAsync(room.RoomId, ct);
            return Result<JoinPrivateRoomResponse>.Failure(start.Code, start.Error ?? "Failed to create game.");
        }

        await _notifier.NotifyMatchFoundAsync(p1, new MatchFoundNotification(Guid.Empty, start.Value!.GameId, p2), ct);
        await _notifier.NotifyMatchFoundAsync(p2, new MatchFoundNotification(Guid.Empty, start.Value!.GameId, p1), ct);

        await _rooms.DeleteRoomAsync(room.RoomId, ct);

        var opponent = userId == p1 ? p2 : p1;
        return Result<JoinPrivateRoomResponse>.Success(new JoinPrivateRoomResponse(start.Value!.GameId, opponent));
    }

    private bool IsInBackpressure()
    {
        // Current approximation (until rooms feature owns game admission fully):
        // use in-memory active games count as signal for backpressure.
        var max = _gameplaySettings.MaxActiveGames;
        var pct = _gameplaySettings.BackpressureThresholdPercent <= 0 ? 100 : _gameplaySettings.BackpressureThresholdPercent;
        var threshold = (int)Math.Ceiling(max * (pct / 100.0));
        return _games.GamesNow >= threshold;
    }
}

internal static class JoinCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/O/1/0

    public static string Generate(int length)
    {
        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }
        return new string(chars);
    }
}

