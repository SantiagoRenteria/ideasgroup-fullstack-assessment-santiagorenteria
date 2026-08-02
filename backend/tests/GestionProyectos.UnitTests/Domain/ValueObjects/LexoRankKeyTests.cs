using GestionProyectos.Domain.ValueObjects;
using Xunit;

namespace GestionProyectos.UnitTests.Domain.ValueObjects;

public class LexoRankKeyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConValorVacio_LanzaExcepcion(string value)
    {
        Assert.Throws<ArgumentException>(() => new LexoRankKey(value));
    }

    [Theory]
    [InlineData("hola mundo")]
    [InlineData("m-t")]
    [InlineData("clave!")]
    public void Constructor_ConCaracteresFueraDelAlfabetoBase62_LanzaExcepcion(string value)
    {
        // Antes de este VO, cualquier string no vacio pasaba como Order valido -- ver
        // arquitectura-decisiones.md §22.
        Assert.Throws<ArgumentException>(() => new LexoRankKey(value));
    }

    [Theory]
    [InlineData("m")]
    [InlineData("aaaaaaaa")]
    [InlineData("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz")]
    public void Constructor_ConAlfabetoValido_CreaLaClave(string value)
    {
        var key = new LexoRankKey(value);

        Assert.Equal(value, key.Value);
    }

    [Fact]
    public void CompareTo_UsaOrdenOrdinal_IgualQueStringCompareOrdinal()
    {
        var m = new LexoRankKey("M");
        var t = new LexoRankKey("T");

        Assert.True(m < t);
        Assert.True(t > m);
        Assert.Equal(string.CompareOrdinal("M", "T") < 0, m < t);
    }

    [Fact]
    public void Equals_ConMismoValor_SonIguales()
    {
        Assert.Equal(new LexoRankKey("m"), new LexoRankKey("m"));
    }
}
