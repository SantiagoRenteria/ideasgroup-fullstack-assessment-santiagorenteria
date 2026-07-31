using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private const string CredencialesInvalidas = "Correo o contraseña incorrectos.";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var usuario = await _usuarioRepository.GetByCorreoAsync(correo, cancellationToken);

        if (usuario is null || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            // Mensaje generico deliberado: no revelar si el correo existe (evita enumeracion de usuarios).
            return Result<LoginResponseDto>.Failure(CredencialesInvalidas);
        }

        var token = _jwtTokenGenerator.Generate(usuario);

        return Result<LoginResponseDto>.Success(
            new LoginResponseDto(token.Value, token.ExpiresAtUtc, usuario.Nombre, usuario.Correo));
    }
}
