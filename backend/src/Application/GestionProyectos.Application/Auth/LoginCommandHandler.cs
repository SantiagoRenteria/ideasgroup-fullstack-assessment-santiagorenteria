using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private const string InvalidCredentials = "Correo o contraseña incorrectos.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Mensaje generico deliberado: no revelar si el correo existe (evita enumeracion de usuarios).
            return Result<LoginResponseDto>.Failure(InvalidCredentials);
        }

        var token = _jwtTokenGenerator.Generate(user);

        return Result<LoginResponseDto>.Success(
            new LoginResponseDto(token.Value, token.ExpiresAtUtc, user.Name, user.Email));
    }
}
