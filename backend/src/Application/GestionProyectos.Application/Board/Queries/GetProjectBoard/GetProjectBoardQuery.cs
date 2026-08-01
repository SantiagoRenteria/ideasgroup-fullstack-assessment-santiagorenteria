using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Board.Queries.GetProjectBoard;

public record GetProjectBoardQuery(Guid ProjectId) : IQuery<Result<BoardResponseDto>>;
