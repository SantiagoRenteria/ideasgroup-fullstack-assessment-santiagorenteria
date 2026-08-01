using GestionProyectos.Application.Columns.Commands.UpdateColumn;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Columns.Commands.UpdateColumn;

public class UpdateColumnCommandHandlerTests
{
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ColumnaExiste_RenombraYReordena()
    {
        var column = new Column(Guid.NewGuid(), Guid.NewGuid(), "Por hacer", 0);
        _columnRepository.GetByIdAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);

        var handler = new UpdateColumnCommandHandler(_columnRepository, _unitOfWork);

        var result = await handler.Handle(new UpdateColumnCommand(column.Id, "En progreso", 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("En progreso", result.Value!.Name);
        Assert.Equal(2, result.Value.Order);
    }

    [Fact]
    public async Task Handle_ColumnaNoExiste_RetornaFailure()
    {
        _columnRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Column?)null);

        var handler = new UpdateColumnCommandHandler(_columnRepository, _unitOfWork);

        var result = await handler.Handle(new UpdateColumnCommand(Guid.NewGuid(), "Nombre", 0), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateColumnCommandHandler.ColumnNotFound, result.Error);
    }
}
