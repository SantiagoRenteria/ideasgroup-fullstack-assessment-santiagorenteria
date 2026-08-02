using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using GestionProyectos.Api.Endpoints;
using GestionProyectos.Application;
using GestionProyectos.Infrastructure;
using GestionProyectos.Infrastructure.Persistence;
using GestionProyectos.Infrastructure.Realtime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

// Bootstrap logger: captura errores durante el arranque (antes de que el host y su
// configuracion esten disponibles), patron recomendado por Serilog.AspNetCore.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando GestionProyectos.Api");

    var builder = WebApplication.CreateBuilder(args);

    // Reemplaza el logger por defecto de ASP.NET Core por Serilog: logging estructurado
    // en JSON en vez de texto plano (ver docs/decisions/arquitectura-decisiones.md §6).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Boton "Authorize" en Swagger UI: pegar el JWT (sin "Bearer ") emitido por
        // POST /api/auth/login para probar los endpoints protegidos.
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Token JWT emitido por POST /api/auth/login."
        });

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };

        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
    });

    // Enums (ProjectStatus, TaskPriority) legibles en JSON ("Planned") en vez de su valor
    // entero subyacente -- mas defendible en la respuesta de la API y en Swagger.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Rate limiting en login (docs/METODOLOGIA.md §9.3): ventana fija de 5 intentos por
    // minuto, particionada por IP -- sin particionar, un cliente agotaria el limite global
    // y bloquearia el login de todos los demas. Vive aca (API, no Infrastructure) porque
    // Microsoft.AspNetCore.RateLimiting solo viene con el shared framework de Sdk.Web.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Log estructurado por request (metodo, ruta, status, duracion) con nivel Warning/Error
    // automatico en 4xx/5xx -- complementa, no reemplaza, el manejo de excepciones de abajo.
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = validationException.Message });
            return;
        }

        // Cuerpo de la peticion ausente o mal formado (JSON invalido, Content-Type incorrecto,
        // etc.): es un error del cliente, no del servidor.
        if (exception is BadHttpRequestException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "El cuerpo de la petición es inválido o está vacío." });
            return;
        }

        Log.Error(exception, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Ocurrio un error inesperado." });
    }));

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    app.MapAuthEndpoints();
    app.MapProjectsEndpoints();
    app.MapColumnsEndpoints();
    app.MapTasksEndpoints();
    app.MapBoardEndpoints();
    app.MapUsersEndpoints();
    app.MapReportsEndpoints();
    app.MapHub<BoardHub>("/hubs/board").RequireAuthorization();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException la lanza `dotnet ef` al construir el host solo para leer
    // metadata de diseno (migraciones) -- no es un fallo real de arranque.
    Log.Fatal(ex, "GestionProyectos.Api termino de forma inesperada durante el arranque");
}
finally
{
    Log.CloseAndFlush();
}
