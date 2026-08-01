using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects;

public class ListProjectsQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();

    [Fact]
    public async Task Handle_ConResultados_RetornaPagedResultConDtosYTotal()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository
            .ListAsync(1, 10, "Nom", null, Arg.Any<CancellationToken>())
            .Returns((new List<Project> { project }, 1));

        var handler = new ListProjectsQueryHandler(_projectRepository);

        var result = await handler.Handle(new ListProjectsQuery(1, 10, "Nom", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_SinResultados_RetornaPagedResultVacio()
    {
        _projectRepository
            .ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<ProjectStatus?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Project>(), 0));

        var handler = new ListProjectsQueryHandler(_projectRepository);

        var result = await handler.Handle(new ListProjectsQuery(1, 10, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalPages);
    }
}
