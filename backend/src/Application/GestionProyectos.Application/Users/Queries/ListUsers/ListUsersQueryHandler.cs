using GestionProyectos.Application.Common.Interfaces;
using MediatR;

namespace GestionProyectos.Application.Users.Queries.ListUsers;

// Sin Result: a diferencia de los demas queries, no hay ningun caso de fallo previsible
// (no depende de un identificador que pueda no existir) -- listar usuarios siempre puede
// devolver una lista, aunque sea vacia.
public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserResponseDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAllAsync(cancellationToken);

        return users.Select(u => u.ToDto()).ToList();
    }
}
