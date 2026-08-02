using GestionProyectos.Application.Auth;
using GestionProyectos.Application.Common.Interfaces;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Auth;

public class LogoutCommandHandlerTests
{
    private readonly ITokenRevocationStore _tokenRevocationStore = Substitute.For<ITokenRevocationStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_RevocaElJtiDelTokenYPersisteCambios()
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        var handler = new LogoutCommandHandler(_tokenRevocationStore, _unitOfWork);

        var result = await handler.Handle(new LogoutCommand(jti, expiresAtUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _tokenRevocationStore.Received(1).RevokeAsync(jti, expiresAtUtc, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
