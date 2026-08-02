using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Xunit;

namespace GestionProyectos.UnitTests.Domain;

public class TaskEntityTests
{
    private static TaskEntity CreateValid(Guid? assigneeId = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Titulo",
        "Descripcion",
        TaskPriority.Medium,
        assigneeId,
        "m",
        DateTime.UtcNow);

    [Fact]
    public void Constructor_ConDatosValidos_CreaTarea()
    {
        var task = CreateValid();

        Assert.Equal("Titulo", task.Title);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void Constructor_ConAssigneeIdAsignado_LoConserva()
    {
        var assigneeId = Guid.NewGuid();

        var task = CreateValid(assigneeId);

        Assert.Equal(assigneeId, task.AssigneeId);
    }

    [Fact]
    public void Constructor_ConColumnIdVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new TaskEntity(Guid.NewGuid(), Guid.Empty, "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConTituloInvalido_LanzaExcepcion(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), title, "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_ConOrdenVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "", DateTime.UtcNow));
    }

    [Fact]
    public void Update_ConDatosValidos_ActualizaCamposDeNegocio()
    {
        var task = CreateValid();
        var assigneeId = Guid.NewGuid();

        task.Update("Nuevo titulo", "Nueva descripcion", TaskPriority.Urgent, assigneeId);

        Assert.Equal("Nuevo titulo", task.Title);
        Assert.Equal("Nueva descripcion", task.Description);
        Assert.Equal(TaskPriority.Urgent, task.Priority);
        Assert.Equal(assigneeId, task.AssigneeId);
    }

    [Fact]
    public void Update_ConTituloInvalido_LanzaExcepcionYNoModificaLaTarea()
    {
        var task = CreateValid();

        Assert.Throws<ArgumentException>(() => task.Update("", "Nueva descripcion", TaskPriority.Urgent, null));

        Assert.Equal("Titulo", task.Title);
    }

    [Fact]
    public void Move_ConDatosValidos_ActualizaColumnaYOrden()
    {
        var task = CreateValid();
        var newColumnId = Guid.NewGuid();

        task.Move(newColumnId, "t");

        Assert.Equal(newColumnId, task.ColumnId);
        Assert.Equal("t", task.Order.Value);
    }

    [Fact]
    public void Move_ConColumnIdVacio_LanzaExcepcionYNoModificaLaTarea()
    {
        var task = CreateValid();
        var originalColumnId = task.ColumnId;

        Assert.Throws<ArgumentException>(() => task.Move(Guid.Empty, "t"));

        Assert.Equal(originalColumnId, task.ColumnId);
    }

    [Fact]
    public void Move_ConOrdenVacio_LanzaExcepcionYNoModificaLaTarea()
    {
        var task = CreateValid();

        Assert.Throws<ArgumentException>(() => task.Move(Guid.NewGuid(), ""));

        Assert.Equal("m", task.Order.Value);
    }

    [Fact]
    public void Delete_MarcaIsDeletedYDeletedAt()
    {
        var task = CreateValid();

        task.Delete();

        Assert.True(task.IsDeleted);
        Assert.NotNull(task.DeletedAt);
    }

    [Fact]
    public void Delete_SiYaEstaEliminada_LanzaExcepcion()
    {
        var task = CreateValid();
        task.Delete();

        Assert.Throws<InvalidOperationException>(() => task.Delete());
    }
}
