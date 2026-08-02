using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestionProyectos.Application.Auth;
using MediatR;

namespace GestionProyectos.Api.Endpoints;

public static class AuthEndpoints
{
    public record LoginRequest(string Email, string Password);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        });

        // Revocacion real del JWT (ADR §16), no solo limpieza en el cliente: el jti y el
        // exp se leen del propio token validado de la peticion, nunca del cuerpo.
        group.MapPost("/logout", async (ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        {
            var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var expClaim = user.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (jti is null || expClaim is null || !long.TryParse(expClaim, out var expUnixSeconds))
                return Results.Unauthorized();

            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;

            await sender.Send(new LogoutCommand(jti, expiresAtUtc), cancellationToken);

            return Results.NoContent();
        }).RequireAuthorization();
    }
}
