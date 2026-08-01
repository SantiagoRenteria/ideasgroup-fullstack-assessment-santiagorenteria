using GestionProyectos.Application.Common.Messaging;

namespace GestionProyectos.Application.Users.Queries.ListUsers;

public record ListUsersQuery : IQuery<IReadOnlyList<UserResponseDto>>;
