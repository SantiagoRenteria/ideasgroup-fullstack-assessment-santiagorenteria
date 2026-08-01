using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Projects;

public record DeleteProjectCommand(Guid Id) : ICommand<Result>;
