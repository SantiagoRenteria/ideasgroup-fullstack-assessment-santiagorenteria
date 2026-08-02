using GestionProyectos.Application.Columns.Queries.ListColumnsByProject;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Columns.Queries.ListColumnsByProject;

public class ListColumnsByProjectQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IColumnRepository _columnRepository = Substitute.For<IColumnRepository>();

    [Fact]
    public async Task Handle_ProyectoExiste_RetornaColumnasOrdenadasPorOrder()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var second = new Column(Guid.NewGuid(), project.Id, "En progreso", 1);
        var first = new Column(Guid.NewGuid(), project.Id, "Por hacer", 0);
        _columnRepository.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Column> { second, first });

        var handler = new ListColumnsByProjectQueryHandler(_projectRepository, _columnRepository, NullLogger<ListColumnsByProjectQueryHandler>.Instance);

        var result = await handler.Handle(new ListColumnsByProjectQuery(project.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Por hacer", "En progreso"], result.Value!.Select(c => c.Name));
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailure()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new ListColumnsByProjectQueryHandler(_projectRepository, _columnRepository, NullLogger<ListColumnsByProjectQueryHandler>.Instance);

        var result = await handler.Handle(new ListColumnsByProjectQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListColumnsByProjectQueryHandler.ProjectNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
