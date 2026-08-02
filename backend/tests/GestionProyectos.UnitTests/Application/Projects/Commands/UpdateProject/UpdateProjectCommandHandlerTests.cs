using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects.Commands.UpdateProject;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ProyectoExiste_ActualizaYRetornaDto()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre viejo", "Descripcion vieja",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var handler = new UpdateProjectCommandHandler(_projectRepository, _unitOfWork, NullLogger<UpdateProjectCommandHandler>.Instance);
        var command = new UpdateProjectCommand(
            project.Id, "Nombre nuevo", "Descripcion nueva",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.InProgress);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nombre nuevo", result.Value!.Name);
        Assert.Equal(ProjectStatus.InProgress, result.Value.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailureYNoPersiste()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new UpdateProjectCommandHandler(_projectRepository, _unitOfWork, NullLogger<UpdateProjectCommandHandler>.Instance);
        var command = new UpdateProjectCommand(
            Guid.NewGuid(), "Nombre", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateProjectCommandHandler.ProjectNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConNombreYaUsadoPorOtroProyecto_RetornaFailureYNoActualiza()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre viejo", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _projectRepository.ExistsByNameAsync("Nombre en uso", project.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new UpdateProjectCommandHandler(_projectRepository, _unitOfWork, NullLogger<UpdateProjectCommandHandler>.Instance);
        var command = new UpdateProjectCommand(
            project.Id, "Nombre en uso", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateProjectCommandHandler.DuplicateName, result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Nombre viejo", project.Name);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
