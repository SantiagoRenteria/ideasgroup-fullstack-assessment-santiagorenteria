using GestionProyectos.Domain.Enums;
using GestionProyectos.Domain.ValueObjects;

namespace GestionProyectos.Domain.Entities;

// Agregado independiente (no raiz de un agregado que incluya Column/TaskEntity): cada uno
// tiene su propio repositorio y su propio limite de concurrencia -- ver
// arquitectura-decisiones.md §22. Por eso no expone una coleccion Columns navegable.
public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public ProjectStatus Status { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Derivada de solo lectura, no mapeada por EF Core (Ignore en ProjectConfiguration):
    // EF Core 8 ComplexProperty no soporta HasData todavia (dotnet/efcore#31254), asi que
    // start_date/end_date siguen siendo columnas planas -- ver arquitectura-decisiones.md §22.
    // El VO igual centraliza la invariante "End >= Start" en el constructor y en Update().
    public DateRange DateRange => new(StartDate, EndDate);

    private Project() { }

    public Project(
        Guid id,
        string name,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        ProjectStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del proyecto es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion del proyecto es obligatoria.", nameof(description));

        var dateRange = new DateRange(startDate, endDate);

        Id = id;
        Name = name;
        Description = description;
        StartDate = dateRange.Start;
        EndDate = dateRange.End;
        Status = status;
    }

    public void Update(string name, string description, DateOnly startDate, DateOnly endDate, ProjectStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del proyecto es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion del proyecto es obligatoria.", nameof(description));

        var dateRange = new DateRange(startDate, endDate);

        Name = name;
        Description = description;
        StartDate = dateRange.Start;
        EndDate = dateRange.End;
        Status = status;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("El proyecto ya fue eliminado.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
