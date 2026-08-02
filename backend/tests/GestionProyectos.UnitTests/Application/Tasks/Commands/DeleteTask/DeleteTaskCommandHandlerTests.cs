using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks.Commands.DeleteTask;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBoardNotifier _boardNotifier = Substitute.For<IBoardNotifier>();

    private DeleteTaskCommandHandler CreateHandler() =>
        new(_taskRepository, _columnRepository, _unitOfWork, _boardNotifier);

    [Fact]
    public async Task Handle_TareaExiste_LaEliminaYPersisteCambios()
    {
        var task = new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);
        var column = new Column(task.ColumnId, Guid.NewGuid(), "Por hacer", 0);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(task.ColumnId, Arg.Any<CancellationToken>()).Returns(column);

        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(task.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TareaNoExiste_RetornaFailureYNoPersisteCambios()
    {
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskEntity?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTaskCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteTaskCommandHandler.TaskNotFound, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Seccion 6.7: la baja debe propagarse al tablero excluyendo al emisor (ADR §15.3).
    [Fact]
    public async Task Handle_TareaEliminada_NotificaAlTableroExcluyendoAlEmisor()
    {
        var task = new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);
        var column = new Column(task.ColumnId, Guid.NewGuid(), "Por hacer", 0);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(task.ColumnId, Arg.Any<CancellationToken>()).Returns(column);

        var handler = CreateHandler();

        await handler.Handle(new DeleteTaskCommand(task.Id, "conn-1"), CancellationToken.None);

        await _boardNotifier.Received(1).TaskDeletedAsync(column.ProjectId, task.Id, task.ColumnId, "conn-1", Arg.Any<CancellationToken>());
    }

    // ADR §15.2: conflicto de concurrencia al eliminar (otra sesion ya la movio/edito).
    [Fact]
    public async Task Handle_ConflictoDeConcurrencia_RetornaFailureYNoNotifica()
    {
        var task = new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyConflictException("conflicto")));

        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteTaskCommandHandler.ConcurrencyConflict, result.Error);
        await _boardNotifier.DidNotReceive().TaskDeletedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
