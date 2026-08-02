using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Common.Behaviors;

// Logging transversal prometido en la tabla de patrones del ADR (§3) pero no implementado
// hasta ahora -- ver docs/decisions/arquitectura-decisiones.md §21. Traza inicio/fin/duracion
// de cada request de MediatR a nivel Debug (volumen alto, sin ruido en produccion por defecto).
// No reemplaza los LogWarning de negocio ya presentes en los handlers criticos (login,
// mutaciones de Tasks): este behavior no conoce el resultado de negocio, solo si el pipeline
// se completo o lanzo una excepcion -- por eso nunca registra el payload del request (evita
// filtrar datos sensibles como la contraseña de LoginCommand), solo el nombre del tipo.
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Iniciando {RequestName}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogDebug(
                "Completado {RequestName} en {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Excepcion no controlada en {RequestName} tras {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
