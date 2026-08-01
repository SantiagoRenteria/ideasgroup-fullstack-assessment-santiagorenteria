using GestionProyectos.Application.Tasks.Commands.CreateTask;
using GestionProyectos.Application.Tasks.Commands.DeleteTask;
using GestionProyectos.Application.Tasks.Commands.MoveTask;
using GestionProyectos.Application.Tasks.Commands.UpdateTask;
using GestionProyectos.Domain.Enums;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class TasksEndpoints
{
    public record CreateTaskRequest(Guid ColumnId, string Title, string Description, TaskPriority Priority, Guid? AssigneeId);
    public record UpdateTaskRequest(string Title, string Description, TaskPriority Priority, Guid? AssigneeId);
    public record MoveTaskRequest(Guid TargetColumnId, int TargetIndex);

    // Header enviado por el cliente Angular con el connectionId de su propio socket
    // SignalR (si el tiene el canal abierto), para que el Handler lo excluya al notificar
    // por tiempo real -- ver docs/decisions/arquitectura-decisiones.md §15.3.
    private const string ConnectionIdHeader = "X-Realtime-Connection-Id";

    private static string? GetConnectionId(HttpContext httpContext) =>
        httpContext.Request.Headers.TryGetValue(ConnectionIdHeader, out var value) ? value.ToString() : null;

    public static void MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapPost("/", async (CreateTaskRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateTaskCommand(request.ColumnId, request.Title, request.Description, request.Priority, request.AssigneeId, GetConnectionId(httpContext)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/tasks/{result.Value!.Id}", result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateTaskCommand(id, request.Title, request.Description, request.Priority, request.AssigneeId, GetConnectionId(httpContext)),
                cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            // Conflicto de concurrencia (xmin, ADR §15.2) es 409: la tarea existe pero
            // otra sesion la modifico primero, distinto de "no encontrado" (404).
            var statusCode = result.Error == UpdateTaskCommandHandler.ConcurrencyConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status404NotFound;

            return Results.Json(new { error = result.Error }, statusCode: statusCode);
        });

        // PATCH, no PUT: traslado por drag&drop (seccion 6.6), distinto de la edicion de
        // datos de negocio -- ver docs/decisions/arquitectura-decisiones.md §14.1.
        group.MapPatch("/{id:guid}/move", async (Guid id, MoveTaskRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new MoveTaskCommand(id, request.TargetColumnId, request.TargetIndex, GetConnectionId(httpContext)),
                cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            // Posicion fuera de rango (400): el estado del tablero que tenia el cliente al
            // iniciar el arrastre ya no es valido. Conflicto de concurrencia (409, ADR
            // §15.2): otra sesion movio/edito la misma tarea primero. Cualquier otro caso
            // (404) dispara la reversion visible que exige 6.6 igual que los anteriores.
            var statusCode = result.Error switch
            {
                MoveTaskCommandHandler.TargetIndexOutOfRange => StatusCodes.Status400BadRequest,
                MoveTaskCommandHandler.ConcurrencyConflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status404NotFound
            };

            return Results.Json(new { error = result.Error }, statusCode: statusCode);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteTaskCommand(id, GetConnectionId(httpContext)), cancellationToken);

            if (result.IsSuccess)
                return Results.NoContent();

            var statusCode = result.Error == DeleteTaskCommandHandler.ConcurrencyConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status404NotFound;

            return Results.Json(new { error = result.Error }, statusCode: statusCode);
        });
    }
}
