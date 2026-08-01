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
}
