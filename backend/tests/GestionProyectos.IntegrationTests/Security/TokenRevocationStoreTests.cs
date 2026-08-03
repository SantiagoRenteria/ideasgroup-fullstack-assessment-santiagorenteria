using GestionProyectos.Infrastructure.Security;
using Xunit;

namespace GestionProyectos.IntegrationTests.Security;

// La purga usa ExecuteDeleteAsync, que traduce a un DELETE ... WHERE real y no pasa por el
// ChangeTracker: no hay forma de verificarlo sin base de datos. Cada bloque usa su propio
// DbContext para reproducir requests HTTP distintos, que es como corre en produccion.
// Ver docs/decisions/arquitectura-decisiones.md §27.3.
[Collection(PostgresCollection.Name)]
public class TokenRevocationStoreTests
{
    private readonly PostgresContainerFixture _fixture;

    public TokenRevocationStoreTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RevokeAsync_DescartaLosJtiYaExpiradosYConservaLosVigentes()
    {
        var jtiExpirado = $"jti-expirado-{Guid.NewGuid()}";
        var jtiVigente = $"jti-vigente-{Guid.NewGuid()}";

        // Primer logout: revoca un token cuyo exp ya paso.
        await using (var primerRequest = _fixture.CreateDbContext())
        {
            await new TokenRevocationStore(primerRequest)
                .RevokeAsync(jtiExpirado, DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);
            await primerRequest.SaveChangesAsync();
        }

        // Segundo logout: su purga es la que debe barrer la fila del primero.
        await using (var segundoRequest = _fixture.CreateDbContext())
        {
            await new TokenRevocationStore(segundoRequest)
                .RevokeAsync(jtiVigente, DateTime.UtcNow.AddHours(1), CancellationToken.None);
            await segundoRequest.SaveChangesAsync();
        }

        await using var verificacion = _fixture.CreateDbContext();
        var store = new TokenRevocationStore(verificacion);

        // Que el expirado desaparezca es la mejora; que el vigente sobreviva es la garantia
        // que no se puede romper -- una purga demasiado ancha desactivaria el logout entero.
        Assert.False(await store.IsRevokedAsync(jtiExpirado, CancellationToken.None));
        Assert.True(await store.IsRevokedAsync(jtiVigente, CancellationToken.None));
    }

    [Fact]
    public async Task IsRevokedAsync_ConUnJtiJamasRevocado_DevuelveFalse()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new TokenRevocationStore(dbContext);

        Assert.False(await store.IsRevokedAsync($"jti-inexistente-{Guid.NewGuid()}", CancellationToken.None));
    }
}
