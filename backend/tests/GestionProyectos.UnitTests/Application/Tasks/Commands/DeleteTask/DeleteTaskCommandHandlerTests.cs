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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_TareaExiste_LaEliminaYPersisteCambios()
    {
        var task = new TaskEntity(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.Low, null, "m", DateTime.UtcNow);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var handler = new DeleteTaskCommandHandler(_taskRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(task.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TareaNoExiste_RetornaFailureYNoPersisteCambios()
    {
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskEntity?)null);

        var handler = new DeleteTaskCommandHandler(_taskRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteTaskCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteTaskCommandHandler.TaskNotFound, result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
