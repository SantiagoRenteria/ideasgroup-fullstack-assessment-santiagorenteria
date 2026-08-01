using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status) : ICommand<Result<ProjectResponseDto>>;
