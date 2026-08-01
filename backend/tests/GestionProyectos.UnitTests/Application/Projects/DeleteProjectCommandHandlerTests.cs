using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects;

public class DeleteProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ProyectoExiste_LoEliminaYPersisteCambios()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var handler = new DeleteProjectCommandHandler(_projectRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _projectRepository.Received(1).Remove(project);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailureYNoPersiste()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new DeleteProjectCommandHandler(_projectRepository, _unitOfWork);

        var result = await handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Proyecto no encontrado.", result.Error);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
