using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Repositories;
using Xunit;

namespace GestionProyectos.IntegrationTests.Repositories;

// El orden de un ORDER BY sin desempate lo decide el motor, no EF: este comportamiento solo
// es observable contra Postgres real, no con un provider in-memory que ordena la lista de
// .NET. Ver docs/decisions/arquitectura-decisiones.md §27.1.
[Collection(PostgresCollection.Name)]
public class ColumnRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ColumnRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListByProjectAsync_ConColumnasQueCompartenOrder_DevuelveUnOrdenEstableYDeterminista()
    {
        await using var dbContext = _fixture.CreateDbContext();

        // Nombre unico por corrida: Project.Name tiene indice unico filtrado por
        // NOT is_deleted y el contenedor se comparte entre los tests de la coleccion.
        var project = new Project(
            Guid.NewGuid(), $"QA orden de columnas {Guid.NewGuid()}", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);

        dbContext.Projects.Add(project);

        // El Order lo elige el cliente (CreateColumnRequest) y nada impide el empate. Los Id
        // estan elegidos para que el desempate NO coincida con el orden de insercion: si la
        // consulta ordenara solo por Order, Postgres tenderia a devolver el orden fisico de
        // la tabla y el resultado seria el inverso al esperado.
        var idMayor = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var idMenor = Guid.Parse("00000000-0000-0000-0000-000000000001");

        dbContext.Columns.Add(new Column(idMayor, project.Id, "Insertada primero", 1));
        dbContext.Columns.Add(new Column(idMenor, project.Id, "Insertada despues", 1));
        await dbContext.SaveChangesAsync();

        var repository = new ColumnRepository(dbContext);

        var primeraLectura = await repository.ListByProjectAsync(project.Id, CancellationToken.None);
        var segundaLectura = await repository.ListByProjectAsync(project.Id, CancellationToken.None);

        Assert.Equal(new[] { idMenor, idMayor }, primeraLectura.Select(c => c.Id));

        // Lo que realmente importa: el tablero no puede intercambiar columnas de sitio entre
        // dos cargas sin que nadie las haya tocado.
        Assert.Equal(primeraLectura.Select(c => c.Id), segundaLectura.Select(c => c.Id));
    }

    [Fact]
    public async Task ListByProjectAsync_ConOrderDistinto_RespetaElOrderPorEncimaDelDesempate()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var project = new Project(
            Guid.NewGuid(), $"QA orden de columnas {Guid.NewGuid()}", "Descripcion",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), ProjectStatus.Planned);

        dbContext.Projects.Add(project);

        // El desempate por Id no debe alterar el criterio principal: la columna con Order
        // menor va primero aunque su Id sea el mayor de los dos.
        var idMayorConOrderMenor = Guid.Parse("ffffffff-0000-0000-0000-000000000002");
        var idMenorConOrderMayor = Guid.Parse("00000000-0000-0000-0000-000000000002");

        dbContext.Columns.Add(new Column(idMenorConOrderMayor, project.Id, "Segunda", 5));
        dbContext.Columns.Add(new Column(idMayorConOrderMenor, project.Id, "Primera", 0));
        await dbContext.SaveChangesAsync();

        var repository = new ColumnRepository(dbContext);

        var columnas = await repository.ListByProjectAsync(project.Id, CancellationToken.None);

        Assert.Equal(new[] { idMayorConOrderMenor, idMenorConOrderMayor }, columnas.Select(c => c.Id));
    }
}
