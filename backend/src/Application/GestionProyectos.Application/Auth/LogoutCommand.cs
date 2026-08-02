using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Auth;

// Jti y ExpiresAtUtc vienen del propio token de la peticion (ya validado por el
// middleware de autenticacion antes de llegar aqui), no de input del usuario -- por eso
// no hay LogoutCommandValidator.
public record LogoutCommand(string Jti, DateTime ExpiresAtUtc) : ICommand<Result>;
