using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Columns;

public static class ColumnMappingExtensions
{
    public static ColumnResponseDto ToDto(this Column column) =>
        new(column.Id, column.ProjectId, column.Name, column.Order);
}
