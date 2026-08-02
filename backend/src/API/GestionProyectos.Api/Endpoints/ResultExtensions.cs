using GestionProyectos.Domain.Common;

namespace GestionProyectos.Api.Endpoints;

// Mapeo unico Result -> HTTP status, por ErrorType (no por contenido del mensaje).
// Ver docs/decisions/arquitectura-decisiones.md §20: antes, cada endpoint repetia un
// switch/ternario comparando result.Error contra una constante de string, con un default
// que caia silenciosamente en 404 para cualquier error no listado explicitamente.
public static class ResultExtensions
{
    public static IResult ToErrorResponse(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("ToErrorResponse solo aplica a un Result fallido.");

        var statusCode = result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            null => throw new InvalidOperationException(
                "Result.Failure sin ErrorType -- Result.Failure siempre exige uno; esto indica un bug, no un caso de negocio."),
            _ => throw new InvalidOperationException($"ErrorType sin mapeo a HTTP status: {result.ErrorType}")
        };

        return Results.Json(new { error = result.Error }, statusCode: statusCode);
    }
}
