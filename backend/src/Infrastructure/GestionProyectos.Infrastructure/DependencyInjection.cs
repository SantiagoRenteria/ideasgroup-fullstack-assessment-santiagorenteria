using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Infrastructure.Persistence;
using GestionProyectos.Infrastructure.Realtime;
using GestionProyectos.Infrastructure.Repositories;
using GestionProyectos.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GestionProyectos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IColumnRepository, ColumnRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IBoardNotifier, SignalRBoardNotifier>();
        services.AddScoped<ITokenRevocationStore, TokenRevocationStore>();

        // JSON en camelCase para paridad con las respuestas REST (JsonStringEnumConverter
        // de Program.cs) -- el cliente Angular usa los mismos DTOs para ambos canales.
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
                };

                // El cliente SignalR de navegador no puede fijar el header Authorization en
                // el handshake de WebSocket -- envia el JWT como query string (access_token)
                // solo para /hubs/*; el resto de la API sigue exigiendo el header Bearer.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },

                    // Blocklist de tokens cerrados por el propio usuario (POST
                    // /api/auth/logout) -- ver docs/decisions/arquitectura-decisiones.md
                    // §16. Corre en cada request autenticado, incluido el hub de SignalR
                    // (comparten esta misma configuracion de AddJwtBearer).
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

                        if (jti is null)
                        {
                            context.Fail("Token invalido.");
                            return;
                        }

                        var revocationStore = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationStore>();

                        if (await revocationStore.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                            context.Fail("La sesión fue cerrada.");
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
