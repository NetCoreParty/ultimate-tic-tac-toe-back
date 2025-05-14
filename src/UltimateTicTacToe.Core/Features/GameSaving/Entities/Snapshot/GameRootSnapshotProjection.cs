using UltimateTicTacToe.Core.Features.Game.Domain.Entities;

namespace UltimateTicTacToe.Core.Features.GameSaving.Entities.Snapshot;

public class GameRootSnapshotProjection
{
    public Guid GameId { get; set; }
    public Guid PlayerXId { get; set; }
    public Guid PlayerOId { get; set; }
    public List<MiniBoardSnapshot> MiniBoards { get; set; } = new();
    public int Status { get; set; }
    public Guid? WinnerId { get; set; }
    public int Version { get; set; }
}

public class MiniBoardSnapshot
{
    public int Row { get; set; }
    public int Col { get; set; }
    public PlayerFigure? Winner { get; set; }
    public List<CellSnapshot> Cells { get; set; } = new();
}

public class CellSnapshot
{
    public int Row { get; set; }
    public int Col { get; set; }
    public string Figure { get; set; } = string.Empty;
}

public class StoredSnapshot
{
    public Guid GameId { get; set; }

    /// <summary>
    /// Number of events when this snapshot was taken
    /// </summary>
    public int Version { get; set; }

    public string StateJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SnapshotCause Cause { get; set; }
}

public enum SnapshotCause
{
    /// <summary>
    /// Event count-based performance optimization
    /// </summary>
    PeriodicThresholdReached,
    /// <summary>
    /// Snapshot at important game milestone
    /// </summary>
    MiniBoardWon,
    /// <summary>
    /// Critical final state
    /// </summary>
    GameWon,
    /// <summary>
    /// Debugging, admin tools, or user-triggered snapshot (e.g. Save Game Feature)
    /// </summary>
    Manual
}