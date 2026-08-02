using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Application.Tasks.Commands.CreateTask;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandlerTests
{
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBoardNotifier _boardNotifier = Substitute.For<IBoardNotifier>();

    private static Column CreateColumn() => new(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);

    private CreateTaskCommandHandler CreateHandler() =>
        new(_columnRepository, _taskRepository, _unitOfWork, _boardNotifier, NullLogger<CreateTaskCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ColumnaVacia_CreaTareaConClaveInicial()
    {
        var column = CreateColumn();
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _taskRepository.ListByColumnAsync(column.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity>());

        var handler = CreateHandler();
        var command = new CreateTaskCommand(column.Id, "Titulo", "Descripcion", TaskPriority.Medium, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Titulo", result.Value!.Title);
        Assert.False(string.IsNullOrEmpty(result.Value.Order));
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ColumnaConTareas_InsertaAlFinalConClaveMayorQueLaUltima()
    {
        var column = CreateColumn();
        var existing = new TaskEntity(Guid.NewGuid(), column.Id, "Existente", "Desc", TaskPriority.Low, null, "m", DateTime.UtcNow);
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _taskRepository.ListByColumnAsync(column.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity> { existing });

        var handler = CreateHandler();
        var command = new CreateTaskCommand(column.Id, "Nueva", "Descripcion", TaskPriority.Medium, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(string.CompareOrdinal(existing.Order, result.Value!.Order) < 0);
    }

    [Fact]
    public async Task Handle_ColumnaNoExiste_RetornaFailureYNoCreaTarea()
    {
        _columnRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Column?)null);

        var handler = CreateHandler();
        var command = new CreateTaskCommand(Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Medium, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateTaskCommandHandler.ColumnNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskEntity>(), Arg.Any<CancellationToken>());
    }

    // Seccion 6.7: alta de tarea debe propagarse por tiempo real al resto de sesiones del
    // mismo tablero (proyecto), excluyendo la conexion del propio emisor (ADR §15.3).
    [Fact]
    public async Task Handle_TareaCreada_NotificaAlTableroExcluyendoAlEmisor()
    {
        var column = CreateColumn();
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _taskRepository.ListByColumnAsync(column.Id, Arg.Any<CancellationToken>()).Returns(new List<TaskEntity>());

        var handler = CreateHandler();
        var command = new CreateTaskCommand(column.Id, "Titulo", "Descripcion", TaskPriority.Medium, null, "conn-1");

        await handler.Handle(command, CancellationToken.None);

        await _boardNotifier.Received(1).TaskCreatedAsync(
            column.ProjectId,
            Arg.Is<TaskResponseDto>(dto => dto.Title == "Titulo"),
            "conn-1",
            Arg.Any<CancellationToken>());
    }
}
