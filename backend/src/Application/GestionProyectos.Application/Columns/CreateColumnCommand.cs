using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns;

public record CreateColumnCommand(Guid ProjectId, string Name, int Order) : ICommand<Result<ColumnResponseDto>>;
