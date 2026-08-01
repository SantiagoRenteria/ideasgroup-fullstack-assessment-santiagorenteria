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

    public static void MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapPost("/", async (CreateTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateTaskCommand(request.ColumnId, request.Title, request.Description, request.Priority, request.AssigneeId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/tasks/{result.Value!.Id}", result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateTaskCommand(id, request.Title, request.Description, request.Priority, request.AssigneeId),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status404NotFound);
        });

        // PATCH, no PUT: traslado por drag&drop (seccion 6.6), distinto de la edicion de
        // datos de negocio -- ver docs/decisions/arquitectura-decisiones.md §14.1.
        group.MapPatch("/{id:guid}/move", async (Guid id, MoveTaskRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MoveTaskCommand(id, request.TargetColumnId, request.TargetIndex), cancellationToken);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            // Posicion fuera de rango es un error del cliente (400): el estado del
            // tablero que tenia el cliente al iniciar el arrastre ya no es valido.
            // Distinto de "no encontrado" (404), que dispara la reversion visible que
            // exige 6.6 igual que cualquier otro error.
            var statusCode = result.Error == MoveTaskCommandHandler.TargetIndexOutOfRange
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status404NotFound;

            return Results.Json(new { error = result.Error }, statusCode: statusCode);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteTaskCommand(id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status404NotFound);
        });
    }
}
