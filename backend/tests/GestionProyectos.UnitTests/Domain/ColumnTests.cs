using GestionProyectos.Domain.Entities;
using Xunit;

namespace GestionProyectos.UnitTests.Domain;

public class ColumnTests
{
    [Fact]
    public void Constructor_ConDatosValidos_CreaColumna()
    {
        var projectId = Guid.NewGuid();

        var column = new Column(Guid.NewGuid(), projectId, "Por hacer", 0);

        Assert.Equal(projectId, column.ProjectId);
        Assert.Equal("Por hacer", column.Name);
        Assert.Equal(0, column.Order);
    }

    [Fact]
    public void Constructor_ConProjectIdVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new Column(Guid.NewGuid(), Guid.Empty, "Por hacer", 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreInvalido_LanzaExcepcion(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Column(Guid.NewGuid(), Guid.NewGuid(), name, 0));
    }

    [Fact]
    public void Constructor_ConOrdenNegativo_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", -1));
    }

    [Fact]
    public void Rename_ConNombreValido_ActualizaNombre()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);

        column.Rename("En progreso");

        Assert.Equal("En progreso", column.Name);
    }

    [Fact]
    public void Rename_ConNombreVacio_LanzaExcepcionYNoModificaNombre()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);

        Assert.Throws<ArgumentException>(() => column.Rename(""));

        Assert.Equal("Por hacer", column.Name);
    }

    [Fact]
    public void MoveTo_ConOrdenValido_ActualizaOrden()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);

        column.MoveTo(3);

        Assert.Equal(3, column.Order);
    }

    [Fact]
    public void MoveTo_ConOrdenNegativo_LanzaExcepcionYNoModificaOrden()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);

        Assert.Throws<ArgumentException>(() => column.MoveTo(-5));

        Assert.Equal(0, column.Order);
    }
}
