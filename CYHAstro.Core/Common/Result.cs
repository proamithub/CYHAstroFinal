namespace CYHAstro.Core.Common;

/// <summary>
/// Discriminated union result type for service operations.
/// Avoids exceptions for expected business failures.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error, string? errorCode = null) { IsSuccess = false; Error = error; ErrorCode = errorCode; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, string? errorCode = null) => new(error, errorCode);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result() { IsSuccess = true; }
    private Result(string error, string? errorCode = null) { IsSuccess = false; Error = error; ErrorCode = errorCode; }

    public static Result Success() => new();
    public static Result Failure(string error, string? errorCode = null) => new(error, errorCode);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error!);

    public static class ErrorCodes
    {
        public const string NotFound = "NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string Conflict = "CONFLICT";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InsufficientBalance = "INSUFFICIENT_BALANCE";
        public const string AstrologerUnavailable = "ASTROLOGER_UNAVAILABLE";
        public const string SessionExpired = "SESSION_EXPIRED";
        public const string PaymentFailed = "PAYMENT_FAILED";
        public const string DuplicateRequest = "DUPLICATE_REQUEST";
        public const string DatabaseError = "DATABASE_ERROR";
        public const string InternalError = "INTERNAL_ERROR";
    }
}
