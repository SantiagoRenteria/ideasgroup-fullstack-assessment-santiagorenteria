using GestionProyectos.Application.Board.Queries.GetProjectBoard;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class BoardEndpoints
{
    public static void MapBoardEndpoints(this WebApplication app)
    {
        // Solo lectura, agregado (ver docs/decisions/arquitectura-decisiones.md §14.4):
        // una sola respuesta con columnas + tareas para la carga inicial y la recarga
        // completa del tablero. Las mutaciones puntuales siguen yendo por /api/tasks.
        app.MapGet("/api/projects/{projectId:guid}/board", async (Guid projectId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProjectBoardQuery(projectId), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status404NotFound);
        })
        .WithTags("Board")
        .RequireAuthorization();
    }
}
