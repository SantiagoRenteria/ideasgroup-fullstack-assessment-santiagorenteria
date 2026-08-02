namespace GestionProyectos.Application.Common.Exceptions;

// Traduce DbUpdateConcurrencyException a un tipo de Application (ADR §15.2): los
// Handlers no deben referenciar EF Core directamente.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }

    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}
