using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Tasks;

public static class TaskMappingExtensions
{
    public static TaskResponseDto ToDto(this TaskEntity task) =>
        new(task.Id, task.ColumnId, task.Title, task.Description, task.Priority, task.AssigneeId, task.Order, task.CreatedAt);
}
