using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Persistence;
using GestionProyectos.Infrastructure.Persistence.Entities;
using GestionProyectos.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestionProyectos.IntegrationTests.Realtime;

// Verifica contra Postgres real el Outbox Pattern (ADR §24): que encolar + guardar el
// cambio de negocio sea atomico (misma transaccion), y que ProcessPendingAsync reclame,
// marque procesado y despache correctamente. FOR UPDATE SKIP LOCKED y el conteo de filas
// procesadas no se pueden verificar con mocks -- dependen del motor real.
[Collection(PostgresCollection.Name)]
public class OutboxProcessorTests
{
    private readonly PostgresContainerFixture _fixture;

    public OutboxProcessorTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed class RecordingBoardNotifier : IBoardNotifier
    {
        public List<string> CalledMethods { get; } = new();
        public TaskResponseDto? LastTask { get; private set; }

        public Task TaskCreatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken)
        {
            CalledMethods.Add(nameof(TaskCreatedAsync));
            LastTask = task;
            return Task.CompletedTask;
        }

        public Task TaskUpdatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken)
        {
            CalledMethods.Add(nameof(TaskUpdatedAsync));
            LastTask = task;
            return Task.CompletedTask;
        }

        public Task TaskDeletedAsync(Guid projectId, Guid taskId, Guid columnId, string? excludeConnectionId, CancellationToken cancellationToken)
        {
            CalledMethods.Add(nameof(TaskDeletedAsync));
            return Task.CompletedTask;
        }

        public Task TaskMovedAsync(Guid projectId, TaskResponseDto task, int targetIndex, string? excludeConnectionId, CancellationToken cancellationToken)
        {
            CalledMethods.Add(nameof(TaskMovedAsync));
            LastTask = task;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Enqueue_YSaveChanges_PersistenElOutboxYElCambioDeNegocioJuntos()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var outboxWriter = new OutboxWriter(dbContext);

        var project = new Project(
            Guid.NewGuid(), $"Proyecto outbox {Guid.NewGuid():N}", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);

        dbContext.Projects.Add(project);
        outboxWriter.Enqueue(OutboxEventTypes.TaskCreated, project.Id, new { message = "prueba" }, excludeConnectionId: null);

        await dbContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateDbContext();
        var persistedProject = await freshContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        var persistedMessage = await freshContext.Set<OutboxMessage>().FirstOrDefaultAsync(m => m.ProjectId == project.Id);

        Assert.NotNull(persistedProject);
        Assert.NotNull(persistedMessage);
        Assert.Null(persistedMessage!.ProcessedAtUtc);

        // Limpieza: se procesa el mensaje para no dejarlo pendiente y contaminar otros
        // tests de esta clase, que comparten el mismo contenedor Postgres y corren en
        // secuencia (misma coleccion xUnit).
        var cleanupProcessor = new OutboxProcessor(freshContext, new RecordingBoardNotifier(), NullLogger<OutboxProcessor>.Instance);
        await cleanupProcessor.ProcessPendingAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessPendingAsync_ConMensajeTaskMovedPendiente_LoMarcaProcesadoYLoDespacha()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var outboxWriter = new OutboxWriter(dbContext);
        var projectId = Guid.NewGuid();

        var taskDto = new TaskResponseDto(
            Guid.NewGuid(), Guid.NewGuid(), "Tarea movida", "Descripcion",
            TaskPriority.Medium, null, "m", DateTime.UtcNow);
        var payload = new TaskMovedOutboxPayload(taskDto, TargetIndex: 2);

        outboxWriter.Enqueue(OutboxEventTypes.TaskMoved, projectId, payload, excludeConnectionId: "conn-1");
        await dbContext.SaveChangesAsync();

        var notifier = new RecordingBoardNotifier();
        var processor = new OutboxProcessor(dbContext, notifier, NullLogger<OutboxProcessor>.Instance);

        var processedCount = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, processedCount);
        Assert.Equal(new[] { nameof(IBoardNotifier.TaskMovedAsync) }, notifier.CalledMethods);
        Assert.Equal("Tarea movida", notifier.LastTask!.Title);

        var message = await dbContext.Set<OutboxMessage>().FirstAsync(m => m.ProjectId == projectId);
        Assert.NotNull(message.ProcessedAtUtc);
    }

    [Fact]
    public async Task ProcessPendingAsync_MensajeYaProcesado_NoLoVuelveADespachar()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var outboxWriter = new OutboxWriter(dbContext);
        var projectId = Guid.NewGuid();

        outboxWriter.Enqueue(OutboxEventTypes.TaskDeleted, projectId, new TaskDeletedOutboxPayload(Guid.NewGuid(), Guid.NewGuid()), null);
        await dbContext.SaveChangesAsync();

        var notifier = new RecordingBoardNotifier();
        var processor = new OutboxProcessor(dbContext, notifier, NullLogger<OutboxProcessor>.Instance);

        var firstRun = await processor.ProcessPendingAsync(CancellationToken.None);
        var secondRun = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, firstRun);
        Assert.Equal(0, secondRun);
        Assert.Single(notifier.CalledMethods);
    }

    [Fact]
    public async Task ProcessPendingAsync_SinMensajesPendientes_DevuelveCero()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var notifier = new RecordingBoardNotifier();
        var processor = new OutboxProcessor(dbContext, notifier, NullLogger<OutboxProcessor>.Instance);

        var processedCount = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(0, processedCount);
        Assert.Empty(notifier.CalledMethods);
    }
}
