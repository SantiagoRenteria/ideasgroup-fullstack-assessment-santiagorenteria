using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Board.Queries.GetProjectBoard;

// Endpoint agregado (ADR §14.4): una sola consulta en vez de que el frontend arme el
// tablero con N llamadas, una por columna.
public class GetProjectBoardQueryHandler : IRequestHandler<GetProjectBoardQuery, Result<BoardResponseDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<GetProjectBoardQueryHandler> _logger;

    public GetProjectBoardQueryHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        ITaskRepository taskRepository,
        ILogger<GetProjectBoardQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _taskRepository = taskRepository;
        _logger = logger;
    }

    public async Task<Result<BoardResponseDto>> Handle(GetProjectBoardQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de consultar el tablero del proyecto inexistente {ProjectId}", request.ProjectId);
            return Result<BoardResponseDto>.Failure(ProjectNotFound, ErrorType.NotFound);
        }

        var columns = await _columnRepository.ListByProjectAsync(request.ProjectId, cancellationToken);
        var tasks = await _taskRepository.ListByProjectAsync(request.ProjectId, cancellationToken);
        var tasksByColumn = tasks.ToLookup(t => t.ColumnId);

        var boardColumns = columns
            .OrderBy(c => c.Order)
            .Select(c => new BoardColumnDto(
                c.Id,
                c.Name,
                c.Order,
                tasksByColumn[c.Id].OrderBy(t => t.Order).Select(t => t.ToDto()).ToList()))
            .ToList();

        return Result<BoardResponseDto>.Success(new BoardResponseDto(project.Id, project.Name, boardColumns));
    }
}
