namespace UltimateTicTacToe.Core;

public class Result<T>
{
    public bool IsSuccess { get; }
    public int Code { get; }
    public string? Error { get; }
    public T? Value { get; }

    private Result(bool isSuccess, T? value, int code, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Code = code;
        Error = error;
    }

    public static Result<T> Success(T value, int code = 200) => new(true, value, code, null);

    public static Result<T> Failure(int code, string error) => new(false, default, code, error);
}