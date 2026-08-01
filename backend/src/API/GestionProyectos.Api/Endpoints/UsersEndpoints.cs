using GestionProyectos.Application.Users.Queries.ListUsers;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        // Solo lectura: alimenta el selector de "responsable" del formulario de tareas
        // (seccion 6.5). No hay CRUD de usuarios en el alcance del enunciado.
        app.MapGet("/api/users", async (ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new ListUsersQuery(), cancellationToken)))
        .WithTags("Users")
        .RequireAuthorization();
    }
}
