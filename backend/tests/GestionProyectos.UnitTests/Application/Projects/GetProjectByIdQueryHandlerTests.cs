using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Entities;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects;

public class GetProjectByIdQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();

    [Fact]
    public async Task Handle_ProyectoExiste_RetornaDto()
    {
        var project = new Project(
            Guid.NewGuid(), "Nombre", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var handler = new GetProjectByIdQueryHandler(_projectRepository);

        var result = await handler.Handle(new GetProjectByIdQuery(project.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(project.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailure()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Project?)null);

        var handler = new GetProjectByIdQueryHandler(_projectRepository);

        var result = await handler.Handle(new GetProjectByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Proyecto no encontrado.", result.Error);
    }
}
