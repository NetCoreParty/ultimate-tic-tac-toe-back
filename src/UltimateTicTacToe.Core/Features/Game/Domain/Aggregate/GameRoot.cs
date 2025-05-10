﻿using UltimateTicTacToe.Core.Features.Game.Domain.Entities;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Core.Features.Game.Domain.Exceptions;

namespace UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;

/// <summary>
/// Aggregate Root for the game. Should be the one that:
/// 1) Owns the full state of the game (including the BigBoard)
/// 2) Tracks and emits domain events
/// 3) Holds invariants (business rules - internal rules that always must be true) and versioning
/// </summary>
public class GameRoot
{
    private GameRoot() { } // Private constructor for rehydration

    public Guid GameId { get; private set; }
    public Guid PlayerXId { get; private set; }
    public Guid PlayerOId { get; private set; }
    public BigBoard Board { get; private set; }
    public GameStatus Status { get; private set; } = GameStatus.IN_PROGRESS;
    public Guid? WinnerId { get; private set; }
    public int Version { get; private set; } = 0;

    private List<IDomainEvent> _uncommittedChanges = new();
    public IReadOnlyCollection<IDomainEvent> UncommittedChanges => _uncommittedChanges.AsReadOnly();

    public static GameRoot CreateNew(Guid gameId, Guid playerXId, Guid playerOId, GameStatus gameStatus = GameStatus.IN_PROGRESS, int version = 0, bool isReplay = false)
    {
        var game = new GameRoot();

        if (isReplay)
        {
            game.GameId = gameId;
            game.PlayerXId = playerXId;
            game.PlayerOId = playerOId;
            game.Board = new BigBoard();
            game.Status = gameStatus;
            game.Version = version;

            return game;
        }

        game.Apply(new GameCreatedEvent(gameId, playerXId, playerOId));

        return game;
    }

    /// <summary>
    /// Play a move in the game. Method is used both in a game and during rehydration (events replay).
    /// </summary>
    /// <param name="isEventReplay">Flag uses during rehydration (or events replay), where you’re replaying events (like CellMarkedEvent) from history and should bypass validations and not emit new events like MiniBoardWonEvent, FullGameWonEvent, etc.</param>
    public void PlayMove(Guid playerId, int boardRow, int boardCol, int cellRow, int cellCol, bool isEventReplay = false)
    {
        var currentFigure = GetCurrentFigure();

        if (isEventReplay)
        {
            // Replay the move without applying domain rules or validations
            //Board.ReplayEvent(boardRow, boardCol, cellRow, cellCol, currentFigure);
            return;
        }

        // Prevent marking a cell if the game has already ended (terminal event emitted but not yet persisted)
        if (UncommittedChanges.Any(e => e is FullGameWonEvent || e is GameDrawnEvent))
            throw new GameNotInProgressException();

        if (Status != GameStatus.IN_PROGRESS)
            throw new GameNotInProgressException();

        var expectedPlayer = currentFigure == PlayerFigure.X ? PlayerXId : PlayerOId;

        if (playerId != expectedPlayer)
            throw new NotYourTurnException();

        if (!Board.IsMiniBoardPlayable(boardRow, boardCol))
            throw new MiniBoardNotPlayableException();

        // Move happens here btw
        if (!Board.TryMakeMove(boardRow, boardCol, cellRow, cellCol, currentFigure))
            throw new InvalidMoveException("Cell already occupied or mini board already won.");

        // Apply marking the cell
        Apply(new CellMarkedEvent(GameId, playerId, boardRow, boardCol, cellRow, cellCol, currentFigure));

        var miniBoard = Board.GetMiniBoard(boardRow, boardCol);

        if (miniBoard.IsWon)
        {
            var winnerId = currentFigure == PlayerFigure.X ? PlayerXId : PlayerOId;
            Apply(new MiniBoardWonEvent(GameId, winnerId, boardRow, boardCol, miniBoard.Winner));
        }
        else if (miniBoard.IsDraw)
            Apply(new MiniBoardDrawnEvent(GameId, boardRow, boardCol));
        else
            return;

        if (Board.Winner != PlayerFigure.None)
            Apply(new FullGameWonEvent(GameId, playerId));
        else if (IsDraw())
            Apply(new GameDrawnEvent(GameId));
        else
            return;
    }

    public static GameRoot Rehydrate(IEnumerable<IDomainEvent> events, GameRoot? gameRoot)
    {
        if (gameRoot == null)
            gameRoot = new GameRoot();

        foreach (var _event in events ?? new List<IDomainEvent>())
        {
            gameRoot.When(_event, isEventReplay: true);
            gameRoot.Version++;
        }            

        return gameRoot;
    }

    private PlayerFigure GetCurrentFigure()
    {
        int totalMoves = Board.GetTotalMoves();
        return totalMoves % 2 == 0 ? PlayerFigure.X : PlayerFigure.O;
    }

    private void Apply(IDomainEvent _event)
    {
        When(_event);
        _uncommittedChanges.Add(_event);
        Version++;
    }

    private void When(IDomainEvent _event, bool isEventReplay = false)
    {
        switch (_event)
        {
            case GameCreatedEvent e:
                GameId = e.GameId;
                PlayerXId = e.PlayerXId;
                PlayerOId = e.PlayerOId;
                Board = new BigBoard();
                Status = GameStatus.IN_PROGRESS;
                break;

            case CellMarkedEvent e:
                if (isEventReplay)
                    Board.TryMakeMove(e.MiniBoardRowId, e.MiniBoardColId, e.CellRowId, e.CellColId, e.PlayerFigure);
                break;

            case FullGameWonEvent e:
                Status = GameStatus.WON;
                WinnerId = e.WinnerId;
                break;

            case GameDrawnEvent:
                Status = GameStatus.DRAW;
                break;
        }
    }

    // Draw if all mini boards are either full or won, and no winner
    private bool IsDraw()
        => Board.AllMiniBoardsCompleted() && Board.Winner == PlayerFigure.None;
}

public enum GameStatus
{
    IN_PROGRESS,
    WON,
    DRAW
}