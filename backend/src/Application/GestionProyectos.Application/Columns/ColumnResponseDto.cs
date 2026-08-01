namespace GestionProyectos.Application.Columns;

public record ColumnResponseDto(Guid Id, Guid ProjectId, string Name, int Order);
