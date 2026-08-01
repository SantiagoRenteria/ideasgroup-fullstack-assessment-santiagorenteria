using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Domain.Entities;

// Nombrada TaskEntity (no Task) para evitar colision con System.Threading.Tasks.Task,
// que aparece en la firma de practicamente todo metodo async del proyecto.
// Convencion documentada en docs/METODOLOGIA.md §7.1.
public class TaskEntity
{
    public Guid Id { get; private set; }
    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public string Order { get; private set; } = null!;
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

        if (string.IsNullOrWhiteSpace(order))
            throw new ArgumentException("El orden de la tarea es obligatorio.", nameof(order));

        Id = id;
        ColumnId = columnId;
        Title = title;
        Description = description;
        Priority = priority;
        AssigneeId = assigneeId;
        Order = order;
        CreatedAt = createdAt;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("La tarea ya fue eliminada.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
