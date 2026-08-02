using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ITokenRevocationStore _tokenRevocationStore;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(ITokenRevocationStore tokenRevocationStore, IUnitOfWork unitOfWork)
    {
        _tokenRevocationStore = tokenRevocationStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _tokenRevocationStore.RevokeAsync(request.Jti, request.ExpiresAtUtc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
