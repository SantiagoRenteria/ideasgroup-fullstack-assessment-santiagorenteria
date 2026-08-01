using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Projects.Commands.DeleteProject;

public record DeleteProjectCommand(Guid Id) : ICommand<Result>;
