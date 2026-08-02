using GestionProyectos.Domain.Enums;
using GestionProyectos.Domain.ValueObjects;

namespace GestionProyectos.Domain.Entities;

// Nombrada TaskEntity (no Task) para evitar colision con System.Threading.Tasks.Task,
// que aparece en la firma de practicamente todo metodo async del proyecto.
// Convencion documentada en docs/METODOLOGIA.md §7.1.
// Agregado independiente de Column y de Project -- ver arquitectura-decisiones.md §22.
public class TaskEntity
{
    public Guid Id { get; private set; }
    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public LexoRankKey Order { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private TaskEntity() { }

    public TaskEntity(
        Guid id,
        Guid columnId,
        string title,
        string description,
        TaskPriority priority,
        Guid? assigneeId,
        string order,
        DateTime createdAt)
    {
        if (columnId == Guid.Empty)
            throw new ArgumentException("La tarea debe pertenecer a una columna.", nameof(columnId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El titulo de la tarea es obligatorio.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion de la tarea es obligatoria.", nameof(description));

        Id = id;
        ColumnId = columnId;
        Title = title;
        Description = description;
        Priority = priority;
        AssigneeId = assigneeId;
        Order = new LexoRankKey(order);
        CreatedAt = createdAt;
    }

    public void Update(string title, string description, TaskPriority priority, Guid? assigneeId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El titulo de la tarea es obligatorio.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion de la tarea es obligatoria.", nameof(description));

        Title = title;
        Description = description;
        Priority = priority;
        AssigneeId = assigneeId;
    }

    // Separado de Update a proposito (ver docs/decisions/arquitectura-decisiones.md §14.1):
    // representa el traslado por drag&drop entre columnas u orden dentro de la misma,
    // no la edicion de los datos de negocio de la tarea.
    public void Move(Guid columnId, string order)
    {
        if (columnId == Guid.Empty)
            throw new ArgumentException("La tarea debe pertenecer a una columna.", nameof(columnId));

        ColumnId = columnId;
        Order = new LexoRankKey(order);
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("La tarea ya fue eliminada.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
