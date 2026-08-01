using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Board.Queries.GetProjectBoard;

// Endpoint agregado (ver docs/decisions/arquitectura-decisiones.md §14.4): una sola
// consulta por repositorio en vez de que el frontend arme el tablero con N llamadas,
// una por columna. Las tareas se traen todas de una vez y se agrupan en memoria por
// columna -- evita repetir el filtro ColumnId en una consulta por columna.
public class GetProjectBoardQueryHandler : IRequestHandler<GetProjectBoardQuery, Result<BoardResponseDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly ITaskRepository _taskRepository;

    public GetProjectBoardQueryHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        ITaskRepository taskRepository)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _taskRepository = taskRepository;
    }

    public async Task<Result<BoardResponseDto>> Handle(GetProjectBoardQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
            return Result<BoardResponseDto>.Failure(ProjectNotFound);

        var columns = await _columnRepository.ListByProjectAsync(request.ProjectId, cancellationToken);
        var tasks = await _taskRepository.ListByProjectAsync(request.ProjectId, cancellationToken);
        var tasksByColumn = tasks.ToLookup(t => t.ColumnId);

        var boardColumns = columns
            .OrderBy(c => c.Order)
            .Select(c => new BoardColumnDto(
                c.Id,
                c.Name,
                c.Order,
                tasksByColumn[c.Id].OrderBy(t => t.Order, StringComparer.Ordinal).Select(t => t.ToDto()).ToList()))
            .ToList();

        return Result<BoardResponseDto>.Success(new BoardResponseDto(project.Id, project.Name, boardColumns));
    }
}
