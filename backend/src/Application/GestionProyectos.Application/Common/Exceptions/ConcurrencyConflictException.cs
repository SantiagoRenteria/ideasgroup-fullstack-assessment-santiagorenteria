namespace GestionProyectos.Application.Common.Exceptions;

// Traduce DbUpdateConcurrencyException (EF Core) a un tipo propio de Application: los
// Handlers no deben referenciar EF Core directamente (ver
// docs/decisions/arquitectura-decisiones.md §15.2) -- Infrastructure.UnitOfWork es quien
// atrapa la excepcion real y relanza esta.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}
