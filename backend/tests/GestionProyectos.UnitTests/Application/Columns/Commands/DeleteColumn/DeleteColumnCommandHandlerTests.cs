using GestionProyectos.Application.Columns.Commands.DeleteColumn;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Columns.Commands.DeleteColumn;

public class DeleteColumnCommandHandlerTests
{
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ColumnaSinTareas_LaEliminaYPersisteCambios()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _columnRepository.HasTasksAsync(column.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteColumnCommandHandler(_columnRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteColumnCommand(column.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(column.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ColumnaConTareas_RetornaFailureYNoLaElimina()
    {
        // Cobertura explicita de la regla de negocio obligatoria (enunciado seccion 6.4):
        // no se permite eliminar una columna que contenga tareas.
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _columnRepository.HasTasksAsync(column.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteColumnCommandHandler(_columnRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteColumnCommand(column.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteColumnCommandHandler.ColumnHasTasks, result.Error);
        Assert.False(column.IsDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ColumnaNoExiste_RetornaFailureSinConsultarTareas()
    {
        _columnRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Column?)null);

        var handler = new DeleteColumnCommandHandler(_columnRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteColumnCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteColumnCommandHandler.ColumnNotFound, result.Error);
        await _columnRepository.DidNotReceive().HasTasksAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
