using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns.Commands.CreateColumn;

public record CreateColumnCommand(Guid ProjectId, string Name, int Order) : ICommand<Result<ColumnResponseDto>>;
