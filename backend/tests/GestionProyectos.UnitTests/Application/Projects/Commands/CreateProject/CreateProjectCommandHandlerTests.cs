using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects.Commands.CreateProject;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
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
        var handler = new CreateProjectCommandHandler(_projectRepository, _unitOfWork, NullLogger<CreateProjectCommandHandler>.Instance);
        var command = new CreateProjectCommand(
            "Migracion ERP", "Descripcion", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Migracion ERP", result.Value!.Name);
        await _projectRepository.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConNombreYaExistente_RetornaFailureYNoCrea()
    {
        _projectRepository.ExistsByNameAsync("Migracion ERP", null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateProjectCommandHandler(_projectRepository, _unitOfWork, NullLogger<CreateProjectCommandHandler>.Instance);
        var command = new CreateProjectCommand(
            "Migracion ERP", "Descripcion", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateProjectCommandHandler.DuplicateName, result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
