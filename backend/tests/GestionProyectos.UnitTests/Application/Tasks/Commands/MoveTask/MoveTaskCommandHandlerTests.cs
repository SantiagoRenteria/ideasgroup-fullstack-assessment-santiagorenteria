using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks.Commands.MoveTask;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.MoveTask;

public class MoveTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static TaskEntity CreateTask(Guid columnId, string order) => new(
        Guid.NewGuid(), columnId, "Titulo", "Descripcion", TaskPriority.Low, null, order, DateTime.UtcNow);

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

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(string.CompareOrdinal(before.Order, task.Order) < 0);
        Assert.True(string.CompareOrdinal(task.Order, after.Order) < 0);
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

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

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

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual("aaaaaaaa", before.Order);
        Assert.NotEqual("aaaaaaab", after.Order);
        Assert.True(string.CompareOrdinal(before.Order, task.Order) < 0);
        Assert.True(string.CompareOrdinal(task.Order, after.Order) < 0);
    }

    [Fact]
    public async Task Handle_TareaNoExiste_RetornaFailure()
    {
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskEntity?)null);

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

        var result = await handler.Handle(new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TaskNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ColumnaDestinoNoExiste_RetornaFailure()
    {
        var task = CreateTask(Guid.NewGuid(), "m");
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Column?)null);

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

        var result = await handler.Handle(new MoveTaskCommand(task.Id, Guid.NewGuid(), 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TargetColumnNotFound, result.Error);
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

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);

        // Solo queda `task` tras excluirse a si misma (0 elementos), asi que el maximo
        // indice valido es 0 -- pedir 5 debe fallar.
        var result = await handler.Handle(new MoveTaskCommand(task.Id, columnId, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveTaskCommandHandler.TargetIndexOutOfRange, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
