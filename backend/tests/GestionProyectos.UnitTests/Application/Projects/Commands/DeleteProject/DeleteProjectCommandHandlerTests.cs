using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects.Commands.DeleteProject;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public DeleteProjectCommandHandlerTests()
    {
        // ExecuteInTransactionAsync es un wrapper: el mock debe invocar de verdad el
        // delegado que le pasa el handler, si no la mutacion (project.Delete(), etc.)
        // nunca corre y los asserts de abajo fallarian sin motivo real.
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());
    }

    private static Project CreateProject() => new(
        Guid.NewGuid(), "Nombre", "Descripcion",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

    [Fact]
    public async Task Handle_ProyectoSinTareas_LoMarcaEliminadoYCascadeaColumnas()
    {
        var project = CreateProject();
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _columnRepository.ProjectHasTasksAsync(project.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteProjectCommandHandler(_projectRepository, _columnRepository, _unitOfWork, NullLogger<DeleteProjectCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(project.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _columnRepository.Received(1).SoftDeleteByProjectAsync(project.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProyectoConTareas_RetornaFailureYNoLoElimina()
    {
        // Cobertura de la regla de negocio (revision documentada en
        // docs/decisions/arquitectura-decisiones.md §7): no se permite eliminar un
        // proyecto que contiene tareas.
        var project = CreateProject();
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _columnRepository.ProjectHasTasksAsync(project.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteProjectCommandHandler(_projectRepository, _columnRepository, _unitOfWork, NullLogger<DeleteProjectCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteProjectCommandHandler.ProjectHasTasks, result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.False(project.IsDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _columnRepository.DidNotReceive().SoftDeleteByProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailureYNoConsultaTareas()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new DeleteProjectCommandHandler(_projectRepository, _columnRepository, _unitOfWork, NullLogger<DeleteProjectCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteProjectCommandHandler.ProjectNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        await _columnRepository.DidNotReceive().ProjectHasTasksAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
