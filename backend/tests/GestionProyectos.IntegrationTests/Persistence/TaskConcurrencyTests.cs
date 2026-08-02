using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionProyectos.IntegrationTests.Persistence;

// Verifica contra Postgres real el conflicto de concurrencia optimista via xmin (ADR
// §15.2). Un mock de repositorio no puede reproducir esto: xmin es una columna de sistema
// gestionada por el motor, no algo que EF simule en memoria.
[Collection(PostgresCollection.Name)]
public class TaskConcurrencyTests
{
    private readonly PostgresContainerFixture _fixture;

    public TaskConcurrencyTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<TaskEntity> SeedTaskAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var project = new Project(
            Guid.NewGuid(), $"Proyecto concurrencia {Guid.NewGuid():N}", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.InProgress);
        var column = new Column(Guid.NewGuid(), project.Id, "Backlog", 0);
        var task = new TaskEntity(
            Guid.NewGuid(), column.Id, "Titulo original", "Descripcion",
            TaskPriority.Medium, null, "m", DateTime.UtcNow);

        dbContext.Projects.Add(project);
        dbContext.Columns.Add(column);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        return task;
    }

    [Fact]
    public async Task DosSesionesModificanLaMismaTarea_LaSegundaEnGuardarLanzaConflictoDeConcurrencia()
    {
        var seededTask = await SeedTaskAsync();

        await using var contextA = _fixture.CreateDbContext();
        await using var contextB = _fixture.CreateDbContext();

        var taskA = await contextA.Tasks.FirstAsync(t => t.Id == seededTask.Id);
        var taskB = await contextB.Tasks.FirstAsync(t => t.Id == seededTask.Id);

        // Dos "sesiones" cargan la misma fila y editan campos distintos -- exactamente el
        // escenario que 6.7 exige que el backend detecte, no solo el tiempo real que lo
        // hace posible.
        taskA.Update("Editado por sesion A", taskA.Description, taskA.Priority, taskA.AssigneeId);
        taskB.Update("Editado por sesion B", taskB.Description, taskB.Priority, taskB.AssigneeId);

        var unitOfWorkA = new UnitOfWork(contextA);
        var unitOfWorkB = new UnitOfWork(contextB);

        await unitOfWorkA.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWorkB.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DosSesionesModificanTareasDistintas_AmbasGuardanSinConflicto()
    {
        // Contraparte del test anterior: dos sesiones moviendo tareas DISTINTAS del mismo
        // proyecto no deben competir por el mismo xmin -- es la razon por la que este
        // proyecto mantiene el token de concurrencia por tarea y no uno solo por proyecto
        // (ver arquitectura-decisiones.md §22.3, agregados independientes).
        var taskOne = await SeedTaskAsync();
        var taskTwo = await SeedTaskAsync();

        await using var contextA = _fixture.CreateDbContext();
        await using var contextB = _fixture.CreateDbContext();

        var taskOneInA = await contextA.Tasks.FirstAsync(t => t.Id == taskOne.Id);
        var taskTwoInB = await contextB.Tasks.FirstAsync(t => t.Id == taskTwo.Id);

        taskOneInA.Update("Editado", taskOneInA.Description, taskOneInA.Priority, taskOneInA.AssigneeId);
        taskTwoInB.Update("Editado", taskTwoInB.Description, taskTwoInB.Priority, taskTwoInB.AssigneeId);

        var unitOfWorkA = new UnitOfWork(contextA);
        var unitOfWorkB = new UnitOfWork(contextB);

        await unitOfWorkA.SaveChangesAsync(CancellationToken.None);
        await unitOfWorkB.SaveChangesAsync(CancellationToken.None);
    }
}
