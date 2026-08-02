using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Users.Queries.ListUsers;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Users.Queries.ListUsers;

public class ListUsersQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private ListUsersQueryHandler CreateHandler() => new(_userRepository);

    [Fact]
    public async Task Handle_MapeaCadaUsuarioASuDtoSinExponerElHashDeContrasena()
    {
        var user = new User(Guid.NewGuid(), "Administrador", "admin@ideasgroup.test", "hash-secreto");
        _userRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var result = await CreateHandler().Handle(new ListUsersQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Name, dto.Name);
        Assert.Equal(user.Email, dto.Email);
    }

    [Fact]
    public async Task Handle_SinUsuarios_RetornaListaVacia()
    {
        _userRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await CreateHandler().Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
