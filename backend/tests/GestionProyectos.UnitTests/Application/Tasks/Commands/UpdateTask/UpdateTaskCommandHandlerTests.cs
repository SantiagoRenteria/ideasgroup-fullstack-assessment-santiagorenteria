using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Application.Tasks.Commands.UpdateTask;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IOutboxSignal _outboxSignal = Substitute.For<IOutboxSignal>();

    private static TaskEntity CreateTask() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);

    private UpdateTaskCommandHandler CreateHandler() =>
        new(_taskRepository, _columnRepository, _unitOfWork, _outboxWriter, _outboxSignal, NullLogger<UpdateTaskCommandHandler>.Instance);

    [Fact]
    public async Task Handle_TareaExiste_ActualizaCamposYPersisteCambios()
    {
        var task = CreateTask();
        var assigneeId = Guid.NewGuid();
        var column = new Column(task.ColumnId, Guid.NewGuid(), "Por hacer", 0);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(task.ColumnId, Arg.Any<CancellationToken>()).Returns(column);

        var handler = CreateHandler();
        var command = new UpdateTaskCommand(task.Id, "Nuevo titulo", "Nueva descripcion", TaskPriority.Urgent, assigneeId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nuevo titulo", result.Value!.Title);
        Assert.Equal(TaskPriority.Urgent, result.Value.Priority);
        Assert.Equal(assigneeId, result.Value.AssigneeId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TareaNoExiste_RetornaFailureYNoPersisteCambios()
    {
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskEntity?)null);

        var handler = CreateHandler();
        var command = new UpdateTaskCommand(Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateTaskCommandHandler.TaskNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Seccion 6.7: la edicion debe propagarse al tablero excluyendo al emisor (ADR §15.3).
    [Fact]
    public async Task Handle_TareaActualizada_NotificaAlTableroExcluyendoAlEmisor()
    {
        var task = CreateTask();
        var column = new Column(task.ColumnId, Guid.NewGuid(), "Por hacer", 0);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(task.ColumnId, Arg.Any<CancellationToken>()).Returns(column);

        var handler = CreateHandler();
        var command = new UpdateTaskCommand(task.Id, "Titulo", "Descripcion", TaskPriority.Low, null, "conn-1");

        await handler.Handle(command, CancellationToken.None);

        _outboxWriter.Received(1).Enqueue(OutboxEventTypes.TaskUpdated, column.ProjectId, Arg.Any<TaskResponseDto>(), "conn-1");
    }

    // ADR §15.2: dos sesiones editando la misma tarea al mismo tiempo -- la segunda en
    // guardar debe recibir un error de negocio (409), no una excepcion sin manejar, y esto
    // dispara la reversion visible que ya exige 6.6.
    [Fact]
    public async Task Handle_ConflictoDeConcurrencia_RetornaFailureYNoNotifica()
    {
        var task = CreateTask();
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyConflictException("conflicto")));

        var handler = CreateHandler();
        var command = new UpdateTaskCommand(task.Id, "Titulo", "Descripcion", TaskPriority.Low, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateTaskCommandHandler.ConcurrencyConflict, result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        // La garantia real es que nunca se avisa al dispatcher tras un conflicto: Signal()
        // solo se llama despues de un SaveChanges exitoso (ver mismo razonamiento en
        // MoveTaskCommandHandlerTests).
        _outboxSignal.DidNotReceive().Signal();
    }
}
