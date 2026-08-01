using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using MediatR;

namespace GestionProyectos.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectResponseDto>>
{
    public const string DuplicateName = "Ya existe un proyecto con este nombre.";

    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectResponseDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _projectRepository.ExistsByNameAsync(request.Name, excludeProjectId: null, cancellationToken))
            return Result<ProjectResponseDto>.Failure(DuplicateName);

        var project = new Project(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Status);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectResponseDto>.Success(project.ToDto());
    }
}
