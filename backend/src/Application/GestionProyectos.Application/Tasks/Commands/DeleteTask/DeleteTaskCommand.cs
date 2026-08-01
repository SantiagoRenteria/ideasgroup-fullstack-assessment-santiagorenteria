using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Tasks.Commands.DeleteTask;

public record DeleteTaskCommand(Guid Id) : ICommand<Result>;
