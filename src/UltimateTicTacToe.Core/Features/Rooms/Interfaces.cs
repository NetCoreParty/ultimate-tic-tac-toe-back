namespace UltimateTicTacToe.Core.Features.Rooms;

public interface IRoomStore
{
    Task<int> CountActiveRoomsAsync(RoomType type, CancellationToken ct);

    Task<RoomDto> CreatePrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct);

    Task<RoomDto?> TryJoinPrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, CancellationToken ct);

    Task<RoomDto?> TryJoinWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct);

    Task<RoomDto> CreateWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct);

    Task<bool> DeleteRoomAsync(Guid roomId, CancellationToken ct);

    Task<IReadOnlyList<RoomDto>> GetExpiredHalfFullWaitingRoomsAsync(DateTime nowUtc, int take, CancellationToken ct);
}

public interface IMatchmakingTicketStore
{
    Task<int> CountQueuedTicketsAsync(DateTime nowUtc, CancellationToken ct);

    Task<MatchmakingTicketDto> CreateQueuedTicketAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct);

    Task<bool> TryMarkMatchedAsync(Guid ticketId, Guid matchedRoomId, Guid gameId, CancellationToken ct);

    Task<bool> TryCancelAsync(Guid ticketId, Guid userId, CancellationToken ct);

    Task<IReadOnlyList<MatchmakingTicketDto>> GetExpiredQueuedTicketsAsync(DateTime nowUtc, int take, CancellationToken ct);

    Task<bool> TryMarkExpiredAsync(Guid ticketId, CancellationToken ct);
}

public interface IRoomMetricsStore
{
    Task IncrementRoomsCreatedAsync(RoomType type, CancellationToken ct);

    Task<(long RegularCreated, long PrivateCreated)> GetRoomsCreatedCountersAsync(CancellationToken ct);
}

public interface IRoomsNotifier
{
    Task NotifyMatchFoundAsync(Guid userId, MatchFoundNotification payload, CancellationToken ct);
    Task NotifyQueueJoinedAsync(Guid userId, QueueForGameResponse payload, CancellationToken ct);
    Task NotifyQueueExpiredAsync(Guid userId, Guid ticketId, CancellationToken ct);
    Task NotifyPrivateRoomCreatedAsync(Guid userId, CreatePrivateRoomResponse payload, CancellationToken ct);
    Task NotifyRoomExpiredAsync(Guid userId, Guid roomId, RoomType type, CancellationToken ct);
}

