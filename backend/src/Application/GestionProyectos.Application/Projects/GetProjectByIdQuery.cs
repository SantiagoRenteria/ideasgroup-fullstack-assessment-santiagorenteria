using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Projects;

public record GetProjectByIdQuery(Guid Id) : IQuery<Result<ProjectResponseDto>>;
