using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Application.Tasks.Commands.MoveTask;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.MoveTask;

public class MoveTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBoardNotifier _boardNotifier = Substitute.For<IBoardNotifier>();

    private static TaskEntity CreateTask(Guid columnId, string order) => new(
        Guid.NewGuid(), columnId, "Titulo", "Descripcion", TaskPriority.Low, null, order, DateTime.UtcNow);

    private MoveTaskCommandHandler CreateHandler() =>
        new(_taskRepository, _columnRepository, _unitOfWork, _boardNotifier, NullLogger<MoveTaskCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ReordenaDentroDeLaMismaColumna_QuedaEntreLosDosVecinos()
    {
        var columnId = Guid.NewGuid();
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        var task = CreateTask(columnId, "a");
        var before = CreateTask(columnId, "M");
        var after = CreateTask(columnId, "T");

        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(columnId, Arg.Any<CancellationToken>()).Returns(column);
        // El repositorio ya excluye o no la tarea movida segun el escenario real; el
        // handler filtra explicitamente por Id, asi que se simula la lista completa.
        _taskRepository.ListByColumnAsync(columnId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskEntity> { before, task, after });

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(before.Order < task.Order);
        Assert.True(task.Order < after.Order);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MueveATareaAColumnaVaciaDeOtroProyecto_ActualizaColumnId()
    {
        var task = CreateTask(Guid.NewGuid(), "m");
        var targetColumn = new Column(Guid.NewGuid(), Guid.NewGuid(), "Hecho", 0);

        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(targetColumn);
        _taskRepository.ListByColumnAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity>());

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(task.Id, targetColumn.Id, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(targetColumn.Id, task.ColumnId);
    }

    [Fact]
    public async Task Handle_GapAgotadoEntreVecinos_RebalanceaLaColumnaCompletaYPreservaElOrden()
    {
        // Dos claves de 8 caracteres adyacentes (sin hueco): fuerza el camino de
        // rebalanceo de TaskOrderingHelper (ver LexoRankServiceTests para el caso
        // equivalente a nivel de Domain). Aqui se verifica que el handler aplique el
        // rebalanceo a las tareas ya existentes en la columna, no solo a la insertada.
        var columnId = Guid.NewGuid();
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        var task = CreateTask(columnId, "a");
        var before = CreateTask(columnId, "aaaaaaaa");
        var after = CreateTask(columnId, "aaaaaaab");

        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(columnId, Arg.Any<CancellationToken>()).Returns(column);
        _taskRepository.ListByColumnAsync(columnId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskEntity> { before, task, after });

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual("aaaaaaaa", before.Order.Value);
        Assert.NotEqual("aaaaaaab", after.Order.Value);
        Assert.True(before.Order < task.Order);
        Assert.True(task.Order < after.Order);
    }

    [Fact]
    public async Task Handle_TareaNoExiste_RetornaFailure()
    {
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskEntity?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TaskNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ColumnaDestinoNoExiste_RetornaFailure()
    {
        var task = CreateTask(Guid.NewGuid(), "m");
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Column?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(task.Id, Guid.NewGuid(), 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TargetColumnNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_IndiceFueraDeRango_RetornaFailureYNoPersisteCambios()
    {
        var columnId = Guid.NewGuid();
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        var task = CreateTask(columnId, "m");

        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(columnId, Arg.Any<CancellationToken>()).Returns(column);
        _taskRepository.ListByColumnAsync(columnId, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity> { task });

        var handler = CreateHandler();

        // Solo queda `task` tras excluirse a si misma (0 elementos), asi que el maximo
        // indice valido es 0 -- pedir 5 debe fallar.
        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TargetIndexOutOfRange, result.Error);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Seccion 6.7: el traslado/reordenamiento debe propagarse al tablero excluyendo al
    // emisor (ADR §15.3), con el TargetIndex original para que las demas sesiones
    // reproduzcan el mismo moveItemInArray/transferArrayItem (ADR §15.5).
    [Fact]
    public async Task Handle_TareaMovida_NotificaAlTableroConTargetIndexExcluyendoAlEmisor()
    {
        var targetColumn = new Column(Guid.NewGuid(), Guid.NewGuid(), "Hecho", 0);
        var task = CreateTask(Guid.NewGuid(), "m");
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(targetColumn);
        _taskRepository.ListByColumnAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity>());

        var handler = CreateHandler();

        await handler.Handle(new MoveTaskCommand(task.Id, targetColumn.Id, 0, "conn-1"), CancellationToken.None);

        await _boardNotifier.Received(1).TaskMovedAsync(targetColumn.ProjectId, Arg.Any<TaskResponseDto>(), 0, "conn-1", Arg.Any<CancellationToken>());
    }

    // ADR §15.2: dos sesiones moviendo la misma tarea al mismo tiempo -- la segunda debe
    // recibir un error de negocio (409), disparando la reversion visible de 6.6.
    [Fact]
    public async Task Handle_ConflictoDeConcurrencia_RetornaFailureYNoNotifica()
    {
        var targetColumn = new Column(Guid.NewGuid(), Guid.NewGuid(), "Hecho", 0);
        var task = CreateTask(Guid.NewGuid(), "m");
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(targetColumn);
        _taskRepository.ListByColumnAsync(targetColumn.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ConcurrencyConflictException("conflicto")));

        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTaskCommand(task.Id, targetColumn.Id, 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.ConcurrencyConflict, result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        await _boardNotifier.DidNotReceive().TaskMovedAsync(Arg.Any<Guid>(), Arg.Any<TaskResponseDto>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
