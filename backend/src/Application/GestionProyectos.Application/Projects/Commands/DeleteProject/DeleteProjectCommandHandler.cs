using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    // Regla de negocio (revision del alcance original, ver docs/decisions/arquitectura-decisiones.md
    // §7): no se permite eliminar un proyecto que contiene tareas, mismo criterio que ya
    // aplica DeleteColumnCommandHandler a nivel de columna individual.
    public const string ProjectHasTasks = "No se puede eliminar un proyecto que contiene tareas.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project is null)
            return Result.Failure(ProjectNotFound);

        if (await _columnRepository.ProjectHasTasksAsync(project.Id, cancellationToken))
            return Result.Failure(ProjectHasTasks);

        // Soft delete: el proyecto se marca eliminado y sus columnas (sin tareas, ya
        // verificado arriba) se marcan en cascada logica. Envuelto en una transaccion
        // explicita porque son dos escrituras separadas (SaveChanges + bulk update) que
        // antes eran una sola sentencia DELETE ... CASCADE.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            project.Delete();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _columnRepository.SoftDeleteByProjectAsync(project.Id, cancellationToken);
        }, cancellationToken);

        return Result.Success();
    }
}
