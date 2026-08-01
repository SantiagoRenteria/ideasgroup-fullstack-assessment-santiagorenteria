using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Projects;

public record ProjectResponseDto(
    Guid Id,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status);
