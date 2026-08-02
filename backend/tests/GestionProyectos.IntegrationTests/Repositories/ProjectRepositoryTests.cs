using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Repositories;
using Xunit;

namespace GestionProyectos.IntegrationTests.Repositories;

// Verifica contra Postgres real el filtro de coincidencia parcial (seccion 6.3) que usa
// EF.Functions.ILike + el indice GIN de pg_trgm (ADR §9). Un provider in-memory no tiene
// la extension pg_trgm ni traduce ILike -- este comportamiento solo es real contra Postgres.
[Collection(PostgresCollection.Name)]
public class ProjectRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ProjectRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListAsync_ConFiltroDeNombreParcialYMinusculas_EncuentraElProyecto()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var project = new Project(
            Guid.NewGuid(), "Migracion de Infraestructura Cloud", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var repository = new ProjectRepository(dbContext);

        // Coincidencia parcial ("infraestructura", no el nombre completo) e insensible a
        // mayusculas -- exactamente el comportamiento que exige la seccion 6.3.
        var (items, totalCount) = await repository.ListAsync(
            page: 1, pageSize: 10, name: "infraestructura", status: null, CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Contains(items, p => p.Id == project.Id);
    }

    [Fact]
    public async Task ListAsync_SinCoincidenciaDeNombre_NoDevuelveElProyecto()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var project = new Project(
            Guid.NewGuid(), "Renovacion de Flota Vehicular", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var repository = new ProjectRepository(dbContext);

        var (items, _) = await repository.ListAsync(
            page: 1, pageSize: 10, name: "infraestructura", status: null, CancellationToken.None);

        Assert.DoesNotContain(items, p => p.Id == project.Id);
    }
}
