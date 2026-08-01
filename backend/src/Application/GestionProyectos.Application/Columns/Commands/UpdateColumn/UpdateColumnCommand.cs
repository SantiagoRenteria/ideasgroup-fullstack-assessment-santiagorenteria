using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns.Commands.UpdateColumn;

public record UpdateColumnCommand(Guid Id, string Name, int Order) : ICommand<Result<ColumnResponseDto>>;
