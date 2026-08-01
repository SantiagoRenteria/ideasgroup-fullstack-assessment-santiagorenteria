using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid Id) : IQuery<Result<ProjectResponseDto>>;
