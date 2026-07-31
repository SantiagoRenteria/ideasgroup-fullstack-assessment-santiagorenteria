using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Auth;

public record LoginCommand(string Correo, string Password) : IRequest<Result<LoginResponseDto>>;
