using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.Tests.Unit.Integration.TestDoubles;

public sealed class InMemoryRoomStore : IRoomStore
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, RoomDto> _rooms = new();

    public Task<int> CountActiveRoomsAsync(RoomType type, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            var count = _rooms.Values.Count(r => r.Type == type && r.Status == RoomStatus.Waiting && r.ExpiresAtUtc > now);
            return Task.FromResult(count);
        }
    }

    public Task<RoomDto> CreatePrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var room = new RoomDto(
                RoomId: Guid.NewGuid(),
                Type: RoomType.Private,
                Status: RoomStatus.Waiting,
                JoinCode: joinCode,
                CreatedAtUtc: nowUtc,
                ExpiresAtUtc: expiresAtUtc,
                Players: new[] { new RoomPlayer(userId, nowUtc) }
            );

            _rooms[room.RoomId] = room;
            return Task.FromResult(room);
        }
    }

    public Task<RoomDto?> TryJoinPrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var room = _rooms.Values.FirstOrDefault(r =>
                r.Type == RoomType.Private &&
                r.Status == RoomStatus.Waiting &&
                r.JoinCode == joinCode &&
                r.ExpiresAtUtc > nowUtc &&
                r.Players.Count == 1 &&
                r.Players[0].UserId != userId);

            if (room == null) return Task.FromResult<RoomDto?>(null);

            var updated = room with
            {
                Status = RoomStatus.Matched,
                Players = new[] { room.Players[0], new RoomPlayer(userId, nowUtc) }
            };

            _rooms[room.RoomId] = updated;
            return Task.FromResult<RoomDto?>(updated);
        }
    }

    public Task<RoomDto?> TryJoinWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var room = _rooms.Values
                .OrderBy(r => r.CreatedAtUtc)
                .FirstOrDefault(r =>
                    r.Type == RoomType.Regular &&
                    r.Status == RoomStatus.Waiting &&
                    r.ExpiresAtUtc > nowUtc &&
                    r.Players.Count == 1 &&
                    r.Players[0].UserId != userId);

            if (room == null) return Task.FromResult<RoomDto?>(null);

            var updated = room with
            {
                Status = RoomStatus.Matched,
                Players = new[] { room.Players[0], new RoomPlayer(userId, nowUtc) }
            };

            _rooms[room.RoomId] = updated;
            return Task.FromResult<RoomDto?>(updated);
        }
    }

    public Task<RoomDto> CreateWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var room = new RoomDto(
                RoomId: Guid.NewGuid(),
                Type: RoomType.Regular,
                Status: RoomStatus.Waiting,
                JoinCode: null,
                CreatedAtUtc: nowUtc,
                ExpiresAtUtc: expiresAtUtc,
                Players: new[] { new RoomPlayer(userId, nowUtc) }
            );

            _rooms[room.RoomId] = room;
            return Task.FromResult(room);
        }
    }

    public Task<bool> DeleteRoomAsync(Guid roomId, CancellationToken ct)
    {
        lock (_lock)
        {
            return Task.FromResult(_rooms.Remove(roomId));
        }
    }

    public Task<IReadOnlyList<RoomDto>> GetExpiredHalfFullWaitingRoomsAsync(DateTime nowUtc, int take, CancellationToken ct)
    {
        lock (_lock)
        {
            var expired = _rooms.Values
                .Where(r => r.Status == RoomStatus.Waiting && r.Players.Count == 1 && r.ExpiresAtUtc <= nowUtc)
                .OrderBy(r => r.ExpiresAtUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<RoomDto>>(expired);
        }
    }
}

public sealed class InMemoryMatchmakingTicketStore : IMatchmakingTicketStore
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, MatchmakingTicketDto> _tickets = new();

    public Task<int> CountQueuedTicketsAsync(DateTime nowUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var count = _tickets.Values.Count(t => t.Status == MatchmakingTicketStatus.Queued && t.ExpiresAtUtc > nowUtc);
            return Task.FromResult(count);
        }
    }

    public Task<MatchmakingTicketDto> CreateQueuedTicketAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        lock (_lock)
        {
            var ticket = new MatchmakingTicketDto(Guid.NewGuid(), userId, MatchmakingTicketStatus.Queued, nowUtc, expiresAtUtc, null, null);
            _tickets[ticket.TicketId] = ticket;
            return Task.FromResult(ticket);
        }
    }

    public Task<bool> TryMarkMatchedAsync(Guid ticketId, Guid matchedRoomId, Guid gameId, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_tickets.TryGetValue(ticketId, out var t)) return Task.FromResult(false);
            if (t.Status != MatchmakingTicketStatus.Queued) return Task.FromResult(false);
            _tickets[ticketId] = t with { Status = MatchmakingTicketStatus.Matched, MatchedRoomId = matchedRoomId, GameId = gameId };
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryCancelAsync(Guid ticketId, Guid userId, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_tickets.TryGetValue(ticketId, out var t)) return Task.FromResult(false);
            if (t.UserId != userId) return Task.FromResult(false);
            if (t.Status != MatchmakingTicketStatus.Queued) return Task.FromResult(false);
            _tickets[ticketId] = t with { Status = MatchmakingTicketStatus.Cancelled };
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<MatchmakingTicketDto>> GetExpiredQueuedTicketsAsync(DateTime nowUtc, int take, CancellationToken ct)
    {
        lock (_lock)
        {
            var expired = _tickets.Values
                .Where(t => t.Status == MatchmakingTicketStatus.Queued && t.ExpiresAtUtc <= nowUtc)
                .OrderBy(t => t.ExpiresAtUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<MatchmakingTicketDto>>(expired);
        }
    }

    public Task<bool> TryMarkExpiredAsync(Guid ticketId, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_tickets.TryGetValue(ticketId, out var t)) return Task.FromResult(false);
            if (t.Status != MatchmakingTicketStatus.Queued) return Task.FromResult(false);
            _tickets[ticketId] = t with { Status = MatchmakingTicketStatus.Expired };
            return Task.FromResult(true);
        }
    }

    // Test helper
    public MatchmakingTicketDto? TryGet(Guid ticketId)
    {
        lock (_lock)
        {
            return _tickets.TryGetValue(ticketId, out var t) ? t : null;
        }
    }
}

public sealed class InMemoryRoomMetricsStore : IRoomMetricsStore
{
    private long _regular;
    private long _private;

    public Task IncrementRoomsCreatedAsync(RoomType type, CancellationToken ct)
    {
        if (type == RoomType.Regular) Interlocked.Increment(ref _regular);
        if (type == RoomType.Private) Interlocked.Increment(ref _private);
        return Task.CompletedTask;
    }

    public Task<(long RegularCreated, long PrivateCreated)> GetRoomsCreatedCountersAsync(CancellationToken ct)
        => Task.FromResult((_regular, _private));
}

