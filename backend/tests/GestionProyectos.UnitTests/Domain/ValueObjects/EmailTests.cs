using GestionProyectos.Domain.ValueObjects;
using Xunit;

namespace GestionProyectos.UnitTests.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Constructor_ConCorreoValido_NormalizaTrimYLowercase()
    {
        var email = new Email("  Admin@IdeasGroup.test  ");

        Assert.Equal("admin@ideasgroup.test", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConCorreoVacio_LanzaExcepcion(string value)
    {
        Assert.Throws<ArgumentException>(() => new Email(value));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("sin-dominio@")]
    [InlineData("@sin-usuario.com")]
    [InlineData("sin-punto@dominio")]
    public void Constructor_ConFormatoInvalido_LanzaExcepcion(string value)
    {
        Assert.Throws<ArgumentException>(() => new Email(value));
    }

    [Fact]
    public void Equals_ConMismoValorNormalizado_SonIguales()
    {
        var a = new Email("Admin@IdeasGroup.test");
        var b = new Email("admin@ideasgroup.test");

        Assert.Equal(a, b);
    }
}
