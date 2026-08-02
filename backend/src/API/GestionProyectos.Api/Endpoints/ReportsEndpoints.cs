using GestionProyectos.Application.Reports.Queries.ExportProjectReport;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects/{projectId:guid}/report", async (Guid projectId, string format, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportProjectReportQuery(projectId, format), cancellationToken);

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
