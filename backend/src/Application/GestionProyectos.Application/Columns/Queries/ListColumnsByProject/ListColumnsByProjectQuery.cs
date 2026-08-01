using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Columns.Queries.ListColumnsByProject;

public record ListColumnsByProjectQuery(Guid ProjectId) : IQuery<Result<IReadOnlyList<ColumnResponseDto>>>;
