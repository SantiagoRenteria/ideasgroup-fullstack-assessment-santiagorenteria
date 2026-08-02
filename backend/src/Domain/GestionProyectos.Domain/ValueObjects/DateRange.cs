namespace GestionProyectos.Domain.ValueObjects;

// Value Object: centraliza la invariante "End >= Start", antes duplicada en el
// constructor y en Update() de Project (ver arquitectura-decisiones.md §22).
public sealed record DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("La fecha de fin prevista no puede ser anterior a la fecha de inicio.", nameof(end));

        Start = start;
        End = end;
    }
}
