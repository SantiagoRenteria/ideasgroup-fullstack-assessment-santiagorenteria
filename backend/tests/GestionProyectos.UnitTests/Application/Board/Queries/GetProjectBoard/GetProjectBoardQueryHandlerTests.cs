using GestionProyectos.Application.Board.Queries.GetProjectBoard;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Board.Queries.GetProjectBoard;

public class GetProjectBoardQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();

    private GetProjectBoardQueryHandler CreateHandler() =>
        new(_projectRepository, _columnRepository, _taskRepository, NullLogger<GetProjectBoardQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailure()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await CreateHandler().Handle(new GetProjectBoardQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetProjectBoardQueryHandler.ProjectNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ProyectoExiste_OrdenaColumnasYAgrupaTareasPorColumnaOrdenadasDentroDeCadaUna()
    {
        var project = new Project(
            Guid.NewGuid(), "Proyecto Demo", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.InProgress);

        // Las columnas se devuelven fuera de orden a proposito -- el handler debe
        // reordenarlas por Order, no confiar en el orden del repositorio.
        var columnHecho = new Column(Guid.NewGuid(), project.Id, "Hecho", 1);
        var columnPorHacer = new Column(Guid.NewGuid(), project.Id, "Por hacer", 0);
        _columnRepository.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Column> { columnHecho, columnPorHacer });

        // Dentro de "Por hacer", las tareas tambien llegan fuera de orden de LexoRank.
        var taskZ = new TaskEntity(Guid.NewGuid(), columnPorHacer.Id, "Tarea Z", "Desc", TaskPriority.Low, null, "z", DateTime.UtcNow);
        var taskA = new TaskEntity(Guid.NewGuid(), columnPorHacer.Id, "Tarea A", "Desc", TaskPriority.High, null, "a", DateTime.UtcNow);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _taskRepository.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new List<TaskEntity> { taskZ, taskA });

        var result = await CreateHandler().Handle(new GetProjectBoardQuery(project.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(project.Id, result.Value!.ProjectId);
        Assert.Equal(project.Name, result.Value.ProjectName);

        Assert.Equal(new[] { "Por hacer", "Hecho" }, result.Value.Columns.Select(c => c.Name));

        var porHacer = result.Value.Columns.Single(c => c.Name == "Por hacer");
        Assert.Equal(new[] { "Tarea A", "Tarea Z" }, porHacer.Tasks.Select(t => t.Title));

        var hecho = result.Value.Columns.Single(c => c.Name == "Hecho");
        Assert.Empty(hecho.Tasks);
    }
}
