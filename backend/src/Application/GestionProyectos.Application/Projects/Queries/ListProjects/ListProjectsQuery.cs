using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Application.Common.Models;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Projects.Queries.ListProjects;

public record ListProjectsQuery(
    int Page,
    int PageSize,
    string? Name,
    ProjectStatus? Status) : IQuery<Result<PagedResult<ProjectResponseDto>>>;
