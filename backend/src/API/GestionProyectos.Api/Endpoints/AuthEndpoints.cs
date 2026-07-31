using GestionProyectos.Application.Auth;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class AuthEndpoints
{
    public record LoginRequest(string Correo, string Password);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LoginCommand(request.Correo, request.Password), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        });
    }
}
