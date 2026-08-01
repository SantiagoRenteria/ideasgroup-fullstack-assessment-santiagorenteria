using GestionProyectos.Application.Tasks;

namespace GestionProyectos.Application.Board;

public record BoardColumnDto(Guid Id, string Name, int Order, IReadOnlyList<TaskResponseDto> Tasks);

public record BoardResponseDto(Guid ProjectId, string ProjectName, IReadOnlyList<BoardColumnDto> Columns);
