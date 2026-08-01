using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects.Commands.CreateProject;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ConDatosValidos_CreaProyectoYPersisteCambios()
    {
        var handler = new CreateProjectCommandHandler(_projectRepository, _unitOfWork);
        var command = new CreateProjectCommand(
            "Migracion ERP", "Descripcion", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Migracion ERP", result.Value!.Name);
        await _projectRepository.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
