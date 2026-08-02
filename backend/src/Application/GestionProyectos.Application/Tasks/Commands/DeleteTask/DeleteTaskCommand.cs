using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Tasks.Commands.DeleteTask;

// ConnectionId: ver CreateTaskCommand / ADR §15.3.
public record DeleteTaskCommand(Guid Id, string? ConnectionId = null) : ICommand<Result>;
