using GestionProyectos.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new(Options.Create(new SecurityOptions { Pepper = "pepper-de-prueba" }));

    [Fact]
    public void Hash_LuegoVerify_ConLaMismaContraseña_RetornaTrue()
    {
        var hash = _hasher.Hash("MiPassword123!");

        Assert.True(_hasher.Verify("MiPassword123!", hash));
    }

    [Fact]
    public void Verify_ConContraseñaIncorrecta_RetornaFalse()
    {
        var hash = _hasher.Hash("MiPassword123!");

        Assert.False(_hasher.Verify("OtraPassword", hash));
    }
}
