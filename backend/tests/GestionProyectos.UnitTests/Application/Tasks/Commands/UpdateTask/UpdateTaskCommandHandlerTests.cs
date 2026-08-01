using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks.Commands.UpdateTask;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static TaskEntity CreateTask() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);

    [Fact]
    public async Task Handle_TareaExiste_ActualizaCamposYPersisteCambios()
    {
        var task = CreateTask();
        var assigneeId = Guid.NewGuid();
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var handler = new UpdateTaskCommandHandler(_taskRepository, _unitOfWork);
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

        var handler = new UpdateTaskCommandHandler(_taskRepository, _unitOfWork);
        var command = new UpdateTaskCommand(Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateTaskCommandHandler.TaskNotFound, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
