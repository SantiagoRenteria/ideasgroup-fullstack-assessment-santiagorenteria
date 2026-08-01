using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Xunit;

namespace GestionProyectos.UnitTests.Domain;

public class ProjectTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 6, 30);

    [Fact]
    public void Constructor_ConDatosValidos_CreaProyecto()
    {
        var project = new Project(Guid.NewGuid(), "Migracion ERP", "Descripcion", Start, End, ProjectStatus.Planned);

        Assert.Equal("Migracion ERP", project.Name);
        Assert.Equal(ProjectStatus.Planned, project.Status);
        Assert.Empty(project.Columns);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreInvalido_LanzaExcepcion(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Project(Guid.NewGuid(), name, "Descripcion", Start, End, ProjectStatus.Planned));
    }

    [Fact]
    public void Constructor_ConDescripcionVacia_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new Project(Guid.NewGuid(), "Nombre", "", Start, End, ProjectStatus.Planned));
    }

    [Fact]
    public void Constructor_ConFechaFinAnteriorAInicio_LanzaExcepcion()
    {
        var endBeforeStart = Start.AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, endBeforeStart, ProjectStatus.Planned));
    }

    [Fact]
    public void Constructor_ConFechaFinIgualAInicio_NoLanzaExcepcion()
    {
        var project = new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, Start, ProjectStatus.Planned);

        Assert.Equal(Start, project.EndDate);
    }

    [Fact]
    public void Update_ConDatosValidos_ActualizaCampos()
    {
        var project = new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, End, ProjectStatus.Planned);

        project.Update("Nuevo nombre", "Nueva descripcion", Start, End.AddDays(30), ProjectStatus.InProgress);

        Assert.Equal("Nuevo nombre", project.Name);
        Assert.Equal("Nueva descripcion", project.Description);
        Assert.Equal(End.AddDays(30), project.EndDate);
        Assert.Equal(ProjectStatus.InProgress, project.Status);
    }

    [Fact]
    public void Update_ConFechaFinAnteriorAInicio_LanzaExcepcionYNoModificaEstado()
    {
        var project = new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, End, ProjectStatus.Planned);

        Assert.Throws<ArgumentException>(() =>
            project.Update("Nombre", "Descripcion", Start, Start.AddDays(-1), ProjectStatus.Completed));

        Assert.Equal(ProjectStatus.Planned, project.Status);
        Assert.Equal(End, project.EndDate);
    }

    [Fact]
    public void Delete_MarcaIsDeletedYDeletedAt()
    {
        var project = new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, End, ProjectStatus.Planned);

        project.Delete();

        Assert.True(project.IsDeleted);
        Assert.NotNull(project.DeletedAt);
    }

    [Fact]
    public void Delete_SiYaEstaEliminado_LanzaExcepcion()
    {
        var project = new Project(Guid.NewGuid(), "Nombre", "Descripcion", Start, End, ProjectStatus.Planned);
        project.Delete();

        Assert.Throws<InvalidOperationException>(() => project.Delete());
    }
}
