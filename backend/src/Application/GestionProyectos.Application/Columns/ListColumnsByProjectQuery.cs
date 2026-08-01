using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns;

public record ListColumnsByProjectQuery(Guid ProjectId) : IQuery<Result<IReadOnlyList<ColumnResponseDto>>>;
