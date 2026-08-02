using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    // Regla de negocio (revision del alcance original, ADR §7): mismo criterio que ya
    // aplica DeleteColumnCommandHandler a nivel de columna individual.
    public const string ProjectHasTasks = "No se puede eliminar un proyecto que contiene tareas.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProjectCommandHandler> _logger;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de eliminar el proyecto inexistente {ProjectId}", request.Id);
            return Result.Failure(ProjectNotFound, ErrorType.NotFound);
        }

        if (await _columnRepository.ProjectHasTasksAsync(project.Id, cancellationToken))
        {
            _logger.LogWarning("Intento de eliminar el proyecto {ProjectId} que contiene tareas", project.Id);
            return Result.Failure(ProjectHasTasks, ErrorType.Conflict);
        }

        // Soft delete en cascada logica, en transaccion explicita (dos escrituras que
        // antes eran un solo DELETE ... CASCADE).
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            project.Delete();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _columnRepository.SoftDeleteByProjectAsync(project.Id, cancellationToken);
        }, cancellationToken);

        return Result.Success();
    }
}
