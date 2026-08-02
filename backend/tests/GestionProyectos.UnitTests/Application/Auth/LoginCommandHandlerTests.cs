using GestionProyectos.Application.Auth;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private LoginCommandHandler CreateHandler() =>
        new(_userRepository, _passwordHasher, _jwtTokenGenerator, NullLogger<LoginCommandHandler>.Instance);

    private static User BuildUser(string email = "admin@ideasgroup.test") =>
        new(Guid.NewGuid(), "Administrador", email, "hash-almacenado");

    [Fact]
    public async Task Handle_CredencialesValidas_RetornaTokenYDatosDelUsuario()
    {
        var user = BuildUser();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("IdeasGroup2026!", user.PasswordHash).Returns(true);
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _jwtTokenGenerator.Generate(user).Returns(new JwtToken("jwt-emitido", expiresAt));

        var result = await CreateHandler().Handle(new LoginCommand(user.Email, "IdeasGroup2026!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-emitido", result.Value!.Token);
        Assert.Equal(user.Name, result.Value.Name);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal(expiresAt, result.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_NormalizaElCorreoAntesDeBuscarlo()
    {
        var user = BuildUser("admin@ideasgroup.test");
        _userRepository.GetByEmailAsync("admin@ideasgroup.test", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtTokenGenerator.Generate(user).Returns(new JwtToken("jwt", DateTime.UtcNow));

        await CreateHandler().Handle(new LoginCommand("  Admin@IdeasGroup.test  ", "IdeasGroup2026!"), CancellationToken.None);

        await _userRepository.Received(1).GetByEmailAsync("admin@ideasgroup.test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsuarioNoExiste_RetornaElMismoMensajeGenericoQuePasswordIncorrecta()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new LoginCommand("desconocido@ideasgroup.test", "cualquiera"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Correo o contraseña incorrectos.", result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    // Seguridad (enumeracion de usuarios, ver el comentario en LoginCommandHandler): el
    // mensaje de error debe ser identico al de "usuario no existe" -- si difiriera, un
    // atacante podria usar la respuesta para confirmar que correo esta registrado.
    [Fact]
    public async Task Handle_PasswordIncorrecta_RetornaElMismoMensajeGenericoQueUsuarioInexistente()
    {
        var user = BuildUser();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("incorrecta", user.PasswordHash).Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand(user.Email, "incorrecta"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Correo o contraseña incorrectos.", result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _jwtTokenGenerator.DidNotReceive().Generate(Arg.Any<User>());
    }
}
