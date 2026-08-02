using GestionProyectos.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Common.Behaviors;

public class LoggingBehaviorTests
{
    private record TestRequest : IRequest<string>;

    private LoggingBehavior<TestRequest, string> CreateBehavior() =>
        new(NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

    [Fact]
    public async Task Handle_CuandoNextTieneExito_RetornaLaRespuestaSinAlterarla()
    {
        var behavior = CreateBehavior();

        var result = await behavior.Handle(
            new TestRequest(),
            () => Task.FromResult("respuesta-del-handler"),
            CancellationToken.None);

        Assert.Equal("respuesta-del-handler", result);
    }

    [Fact]
    public async Task Handle_CuandoNextLanzaExcepcion_RelanzaLaMismaExcepcionSinEnvolverla()
    {
        var behavior = CreateBehavior();
        var excepcionOriginal = new InvalidOperationException("fallo del handler");

        var excepcionCapturada = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TestRequest(),
                () => throw excepcionOriginal,
                CancellationToken.None));

        Assert.Same(excepcionOriginal, excepcionCapturada);
    }
}
