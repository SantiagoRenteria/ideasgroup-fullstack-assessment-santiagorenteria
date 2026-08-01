namespace GestionProyectos.Domain.Entities;

public class Column
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Order { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Column() { }

    public Column(Guid id, Guid projectId, string name, int order)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("La columna debe pertenecer a un proyecto.", nameof(projectId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la columna es obligatorio.", nameof(name));

        if (order < 0)
            throw new ArgumentException("El orden de la columna no puede ser negativo.", nameof(order));

        Id = id;
        ProjectId = projectId;
        Name = name;
        Order = order;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la columna es obligatorio.", nameof(name));

        Name = name;
    }

    public void MoveTo(int order)
    {
        if (order < 0)
            throw new ArgumentException("El orden de la columna no puede ser negativo.", nameof(order));

        Order = order;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("La columna ya fue eliminada.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
