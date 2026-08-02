using GestionProyectos.Application.Columns.Commands.CreateColumn;
using GestionProyectos.Application.Columns.Commands.DeleteColumn;
using GestionProyectos.Application.Columns.Commands.UpdateColumn;
using GestionProyectos.Application.Columns.Queries.ListColumnsByProject;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class ColumnsEndpoints
{
    public record CreateColumnRequest(string Name, int Order);
    public record UpdateColumnRequest(string Name, int Order);

    public static void MapColumnsEndpoints(this WebApplication app)
    {
        var projectColumns = app.MapGroup("/api/projects/{projectId:guid}/columns").WithTags("Columns").RequireAuthorization();

        projectColumns.MapPost("/", async (Guid projectId, CreateColumnRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateColumnCommand(projectId, request.Name, request.Order), cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/columns/{result.Value!.Id}", result.Value)
                : result.ToErrorResponse();
        });

        projectColumns.MapGet("/", async (Guid projectId, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListColumnsByProjectQuery(projectId), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResponse();
        });

        var columns = app.MapGroup("/api/columns").WithTags("Columns").RequireAuthorization();

        columns.MapPut("/{id:guid}", async (Guid id, UpdateColumnRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UpdateColumnCommand(id, request.Name, request.Order), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToErrorResponse();
        });

        columns.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteColumnCommand(id), cancellationToken);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToErrorResponse();
        });
    }
}
