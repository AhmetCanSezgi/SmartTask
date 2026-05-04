namespace SmartTask.Domain.Common;

/// <summary>
/// Exception fırlatmak yerine başarı/hata durumunu taşıyan generic Result pattern.
/// Tüm Application komutları bu tipi döner.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool success, string? error) { IsSuccess = success; Error = error; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
