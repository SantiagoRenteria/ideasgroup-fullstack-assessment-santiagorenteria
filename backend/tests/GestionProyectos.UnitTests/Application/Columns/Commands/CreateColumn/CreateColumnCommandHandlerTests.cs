using GestionProyectos.Application.Columns.Commands.CreateColumn;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Columns.Commands.CreateColumn;

public class CreateColumnCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Project CreateProject() => new(
        Guid.NewGuid(), "Nombre", "Descripcion",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

    [Fact]
    public async Task Handle_ProyectoExiste_CreaColumnaYPersisteCambios()
    {
        var project = CreateProject();
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var handler = new CreateColumnCommandHandler(_projectRepository, _columnRepository, _unitOfWork);
        var command = new CreateColumnCommand(project.Id, "Por hacer", 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Por hacer", result.Value!.Name);
        await _columnRepository.Received(1).AddAsync(Arg.Any<Column>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailureYNoCreaColumna()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new CreateColumnCommandHandler(_projectRepository, _columnRepository, _unitOfWork);
        var command = new CreateColumnCommand(Guid.NewGuid(), "Por hacer", 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateColumnCommandHandler.ProjectNotFound, result.Error);
        await _columnRepository.DidNotReceive().AddAsync(Arg.Any<Column>(), Arg.Any<CancellationToken>());
    }
}
