using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns.Commands.DeleteColumn;

public record DeleteColumnCommand(Guid Id) : ICommand<Result>;
