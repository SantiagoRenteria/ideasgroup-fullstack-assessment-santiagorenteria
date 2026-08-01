using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns;

public record DeleteColumnCommand(Guid Id) : ICommand<Result>;
