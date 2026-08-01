using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Projects.Commands.UpdateProject;

public record UpdateProjectCommand(
    Guid Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status) : ICommand<Result<ProjectResponseDto>>;
