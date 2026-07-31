using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Auth;

public record LoginCommand(string Correo, string Password) : ICommand<Result<LoginResponseDto>>;
