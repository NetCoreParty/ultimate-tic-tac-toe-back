namespace UltimateTicTacToe.Core.Features.Rooms;

public enum RoomType
{
    Regular = 0,
    Private = 1
}

public enum RoomStatus
{
    Waiting = 0,
    Matched = 1,
    Expired = 2
}

public enum MatchmakingTicketStatus
{
    Queued = 0,
    Matched = 1,
    Cancelled = 2,
    Expired = 3
}

public record RoomPlayer(Guid UserId, DateTime JoinedAtUtc);

public record RoomDto(
    Guid RoomId,
    RoomType Type,
    RoomStatus Status,
    string? JoinCode,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    IReadOnlyList<RoomPlayer> Players
);

public record MatchmakingTicketDto(
    Guid TicketId,
    Guid UserId,
    MatchmakingTicketStatus Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    Guid? MatchedRoomId,
    Guid? GameId
);

public record CreatePrivateRoomResponse(Guid RoomId, string JoinCode, DateTime ExpiresAtUtc);
public record QueueForGameResponse(Guid TicketId, DateTime ExpiresAtUtc);
public record JoinPrivateRoomResponse(Guid GameId, Guid OpponentUserId);
public record MatchFoundNotification(Guid TicketId, Guid GameId, Guid OpponentUserId);

