namespace UltimateTicTacToe.Core.Features.GameManagement;

public record GetMovesHistoryQueryHandler(Guid GameId, int Skip, int Take);