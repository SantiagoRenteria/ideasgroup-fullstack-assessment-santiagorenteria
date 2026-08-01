using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns;

public record UpdateColumnCommand(Guid Id, string Name, int Order) : ICommand<Result<ColumnResponseDto>>;
