using GestionProyectos.Application.Projects.Commands.CreateProject;
using GestionProyectos.Application.Projects.Commands.DeleteProject;
using GestionProyectos.Application.Projects.Commands.UpdateProject;
using GestionProyectos.Application.Projects.Queries.GetProjectById;
using GestionProyectos.Application.Projects.Queries.ListProjects;
using GestionProyectos.Domain.Enums;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class ProjectsEndpoints
{
    public record CreateProjectRequest(string Name, string Description, DateOnly StartDate, DateOnly EndDate, ProjectStatus Status);
    public record UpdateProjectRequest(string Name, string Description, DateOnly StartDate, DateOnly EndDate, ProjectStatus Status);

    public static void MapProjectsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        group.MapPost("/", async (CreateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CreateProjectCommand(request.Name, request.Description, request.StartDate, request.EndDate, request.Status),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value!.Id}", result.Value)
                : result.ToErrorResponse();
        });

        group.MapGet("/", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 10,
            string? name = null,
            string? status = null) =>
        {
            // string? en vez de ProjectStatus?: el binding nativo rechaza "status=" vacio
            // con 400, cuando deberia significar "sin filtro". Solo se rechaza un valor
            // realmente invalido.
            ProjectStatus? statusFilter = null;

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsedStatus))
                    return Results.Json(new { error = "El estado del proyecto no es válido." }, statusCode: StatusCodes.Status400BadRequest);

                statusFilter = parsedStatus;
            }

            var result = await sender.Send(new ListProjectsQuery(page, pageSize, name, statusFilter), cancellationToken);

            return Results.Ok(result.Value);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProjectByIdQuery(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResponse();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateProjectCommand(id, request.Name, request.Description, request.StartDate, request.EndDate, request.Status),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResponse();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteProjectCommand(id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToErrorResponse();
        });
    }
}
