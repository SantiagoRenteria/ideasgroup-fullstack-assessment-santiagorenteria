using GestionProyectos.Infrastructure.Realtime;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure.Realtime;

public class BoardPresenceTrackerTests
{
    private readonly BoardPresenceTracker _tracker = new();
    private readonly Guid _projectId = Guid.NewGuid();

    [Fact]
    public void Join_UnaConexion_DevuelveElNombreDeEsaConexion()
    {
        var result = _tracker.Join(_projectId, "conn-1", "Administrador");

        Assert.Equal(new[] { "Administrador" }, result);
    }

    [Fact]
    public void Join_VariasConexionesDeUsuariosDistintos_DevuelveTodosLosNombres()
    {
        _tracker.Join(_projectId, "conn-1", "Administrador");

        var result = _tracker.Join(_projectId, "conn-2", "Evaluador");

        Assert.Equal(new[] { "Administrador", "Evaluador" }, result);
    }

    // Mismo usuario con dos pestañas abiertas en el mismo tablero: dos conexiones, pero
    // debe aparecer una sola vez en el indicador (ver comentario en NamesOf).
    [Fact]
    public void Join_MismoUsuarioConDosConexiones_NoLoDuplicaEnElResultado()
    {
        _tracker.Join(_projectId, "conn-1", "Administrador");

        var result = _tracker.Join(_projectId, "conn-2", "Administrador");

        Assert.Equal(new[] { "Administrador" }, result);
    }

    [Fact]
    public void Leave_QuitaSoloEsaConexionYDejaAlRestoDelTablero()
    {
        _tracker.Join(_projectId, "conn-1", "Administrador");
        _tracker.Join(_projectId, "conn-2", "Evaluador");

        var result = _tracker.Leave(_projectId, "conn-1");

        Assert.Equal(new[] { "Evaluador" }, result);
    }

    [Fact]
    public void Leave_TableroSinRegistrarConexiones_DevuelveListaVaciaSinLanzar()
    {
        var result = _tracker.Leave(Guid.NewGuid(), "conn-inexistente");

        Assert.Empty(result);
    }

    [Fact]
    public void RemoveConnection_ConexionUnidaAUnTablero_LaQuitaYDevuelveElProyectoYElRestante()
    {
        _tracker.Join(_projectId, "conn-1", "Administrador");
        _tracker.Join(_projectId, "conn-2", "Evaluador");

        var result = _tracker.RemoveConnection("conn-1");

        Assert.NotNull(result);
        Assert.Equal(_projectId, result!.Value.ProjectId);
        Assert.Equal(new[] { "Evaluador" }, result.Value.RemainingUsers);
    }

    [Fact]
    public void RemoveConnection_ConexionNoRegistrada_DevuelveNull()
    {
        var result = _tracker.RemoveConnection("conn-nunca-unida");

        Assert.Null(result);
    }

    [Fact]
    public void RemoveConnection_LlamadoDosVecesParaLaMismaConexion_LaSegundaDevuelveNull()
    {
        _tracker.Join(_projectId, "conn-1", "Administrador");
        _tracker.RemoveConnection("conn-1");

        var result = _tracker.RemoveConnection("conn-1");

        Assert.Null(result);
    }
}
