using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Reports;
using Xunit;

namespace GestionProyectos.IntegrationTests.Reports;

// Verifica contra Postgres real lo que arquitectura-decisiones.md §18.2 dice haber
// "verificado a mano": un proyecto sin tareas debe producir 1 fila con campos de tarea en
// null (no 0 filas, que seria indistinguible de "el proyecto no existe"). El LEFT JOIN
// encadenado (Project -> Columns -> Tasks -> User) solo se puede probar de verdad contra
// el motor real: un provider in-memory no ejecuta la traduccion SQL que este test cubre.
[Collection(PostgresCollection.Name)]
public class ProjectReportRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ProjectReportRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetReportAsync_ProyectoSinTareas_DevuelveUnaFilaConTareasVacias()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var project = new Project(
            Guid.NewGuid(), "Proyecto sin tareas", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);
        var column = new Column(Guid.NewGuid(), project.Id, "Backlog", 0);

        dbContext.Projects.Add(project);
        dbContext.Columns.Add(column);
        await dbContext.SaveChangesAsync();

        var repository = new ProjectReportRepository(dbContext);
        var report = await repository.GetReportAsync(project.Id, assigneeId: null, priority: null, CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(project.Id, report!.ProjectId);
        Assert.Empty(report.Tasks);
    }

    [Fact]
    public async Task GetReportAsync_ProyectoInexistente_DevuelveNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new ProjectReportRepository(dbContext);

        var report = await repository.GetReportAsync(Guid.NewGuid(), assigneeId: null, priority: null, CancellationToken.None);

        Assert.Null(report);
    }

    [Fact]
    public async Task GetReportAsync_ProyectoConTareas_LasIncluyeConSuColumnaYResponsable()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var assignee = new User(Guid.NewGuid(), "Responsable", $"{Guid.NewGuid():N}@ideasgroup.test", "hash");
        var project = new Project(
            Guid.NewGuid(), "Proyecto con tareas", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.InProgress);
        var column = new Column(Guid.NewGuid(), project.Id, "Backlog", 0);
        var task = new TaskEntity(
            Guid.NewGuid(), column.Id, "Tarea de prueba", "Descripcion de la tarea",
            TaskPriority.High, assignee.Id, "m", DateTime.UtcNow);

        dbContext.Users.Add(assignee);
        dbContext.Projects.Add(project);
        dbContext.Columns.Add(column);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        var repository = new ProjectReportRepository(dbContext);
        var report = await repository.GetReportAsync(project.Id, assigneeId: null, priority: null, CancellationToken.None);

        Assert.NotNull(report);
        var reportedTask = Assert.Single(report!.Tasks);
        Assert.Equal("Backlog", reportedTask.ColumnName);
        Assert.Equal("Tarea de prueba", reportedTask.Title);
        Assert.Equal("Responsable", reportedTask.AssigneeName);
    }
}
