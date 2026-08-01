using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Projects;

public record UpdateProjectCommand(
    Guid Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status) : ICommand<Result<ProjectResponseDto>>;
