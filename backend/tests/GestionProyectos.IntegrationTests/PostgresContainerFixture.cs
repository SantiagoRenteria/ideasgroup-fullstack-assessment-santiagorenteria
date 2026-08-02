using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GestionProyectos.IntegrationTests;

// Un contenedor Postgres real compartido por toda la suite de integracion (no uno por
// test: levantar el contenedor es el costo caro, no correr las migraciones sobre el).
// Las migraciones reales (incluida la seed data via HasData) se aplican una sola vez en
// InitializeAsync -- ver docs/decisions/arquitectura-decisiones.md §23.
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres real (Testcontainers)";
}
