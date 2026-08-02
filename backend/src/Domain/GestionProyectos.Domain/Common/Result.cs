namespace GestionProyectos.Domain.Common;

// Clasifica el tipo de fallo de negocio para que el mapeo a HTTP status en la API se haga
// por tipo, no por contenido del mensaje de error -- ver arquitectura-decisiones.md §20.
public enum ErrorType
{
    NotFound,
    Conflict,
    Validation,
    Unauthorized
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    protected Result(bool isSuccess, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, null);

    // ErrorType es obligatorio: sin overload que lo omita, para que un nuevo Failure() sin
    // clasificar sea un error de compilación, no un 404 silencioso en el endpoint.
    public static Result Failure(string error, ErrorType errorType) => new(false, error, errorType);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? error, ErrorType? errorType) : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null, null);
    public static new Result<T> Failure(string error, ErrorType errorType) => new(default, false, error, errorType);
}
