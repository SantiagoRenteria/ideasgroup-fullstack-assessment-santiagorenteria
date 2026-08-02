using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ITokenRevocationStore _tokenRevocationStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        ITokenRevocationStore tokenRevocationStore,
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger)
    {
        _tokenRevocationStore = tokenRevocationStore;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _tokenRevocationStore.RevokeAsync(request.Jti, request.ExpiresAtUtc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Unico handler sin ruta de fallo; se logea el evento en si (cierre de sesion,
        // relevante para auditoria) en vez de un LogWarning que nunca se dispararia.
        _logger.LogInformation("Sesion cerrada, token revocado (jti {Jti})", request.Jti);

        return Result.Success();
    }
}
