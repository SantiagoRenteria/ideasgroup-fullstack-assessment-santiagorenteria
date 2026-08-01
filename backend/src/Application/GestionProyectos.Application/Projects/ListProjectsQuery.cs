using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Application.Common.Models;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Projects;

public record ListProjectsQuery(
    int Page,
    int PageSize,
    string? Name,
    ProjectStatus? Status) : IQuery<Result<PagedResult<ProjectResponseDto>>>;
