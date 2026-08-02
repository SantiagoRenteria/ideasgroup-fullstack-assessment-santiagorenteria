using GestionProyectos.Application.Reports.Queries.ExportProjectReport;
using GestionProyectos.Domain.Enums;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects/{projectId:guid}/report", async (
            Guid projectId,
            string format,
            ISender sender,
            CancellationToken cancellationToken,
            string? assigneeId = null,
            string? priority = null) =>
        {
            // string? en vez de Guid?/TaskPriority? a proposito, mismo motivo que
            // ProjectsEndpoints.status: el binding nativo devuelve 400 ante un query param
            // presente pero vacio (como limpiar un combo en el frontend), y eso deberia
            // significar "sin filtro", no un error de formato.
            Guid? assigneeFilter = null;

            if (!string.IsNullOrWhiteSpace(assigneeId))
            {
                if (!Guid.TryParse(assigneeId, out var parsedAssigneeId))
                    return Results.Json(new { error = "El responsable no es válido." }, statusCode: StatusCodes.Status400BadRequest);

                assigneeFilter = parsedAssigneeId;
            }

            TaskPriority? priorityFilter = null;

            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (!Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsedPriority))
                    return Results.Json(new { error = "La prioridad no es válida." }, statusCode: StatusCodes.Status400BadRequest);

                priorityFilter = parsedPriority;
            }

            var result = await sender.Send(new ExportProjectReportQuery(projectId, format, assigneeFilter, priorityFilter), cancellationToken);

            if (!result.IsSuccess)
            {
                var statusCode = result.Error == ExportProjectReportQueryHandler.ProjectNotFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                return Results.Json(new { error = result.Error }, statusCode: statusCode);
            }

            return Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        })
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
