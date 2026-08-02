using System.Reflection;
using FluentValidation;
using GestionProyectos.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace GestionProyectos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Orden importa: cada behavior envuelve al siguiente. Logging va primero para que
        // tambien trace requests que fallan en ValidationBehavior (ver ADR §21).
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
