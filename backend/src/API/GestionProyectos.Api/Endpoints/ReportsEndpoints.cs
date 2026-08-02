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
            // string? en vez de tipos fuertes (mismo motivo que ProjectsEndpoints.status):
            // un query param vacio debe significar "sin filtro", no un 400 de binding.
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

            return result.IsSuccess
                ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
                : result.ToErrorResponse();
        })
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
