namespace UltimateTicTacToe.Core.Features.Game.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class GameNotFoundException : DomainException
{
    public GameNotFoundException() : base("Game was not found.") { }
}

public class GameNotInProgressException : DomainException
{
    public GameNotInProgressException() : base("Game is not in progress.") { }
}

public class NotYourTurnException : DomainException
{
    public NotYourTurnException() : base("It is not your turn to play.") { }
}

public class InvalidMoveException : DomainException
{
    public InvalidMoveException(string reason) : base($"Invalid move: {reason}") { }
}

public class MiniBoardNotPlayableException : DomainException
{
    public MiniBoardNotPlayableException() : base("Selected mini board is not playable.") { }
}