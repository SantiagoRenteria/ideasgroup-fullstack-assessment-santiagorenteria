using GestionProyectos.Domain.ValueObjects;
using Xunit;

namespace GestionProyectos.UnitTests.Domain.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_ConFechaFinPosteriorAInicio_CreaElRango()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 6, 30);

        var range = new DateRange(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void Constructor_ConFechaFinIgualAInicio_NoLanzaExcepcion()
    {
        var date = new DateOnly(2026, 1, 1);

        var range = new DateRange(date, date);

        Assert.Equal(date, range.End);
    }

    [Fact]
    public void Constructor_ConFechaFinAnteriorAInicio_LanzaExcepcion()
    {
        var start = new DateOnly(2026, 1, 1);

        Assert.Throws<ArgumentException>(() => new DateRange(start, start.AddDays(-1)));
    }

    [Fact]
    public void Equals_ConMismasFechas_SonIguales()
    {
        var a = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var b = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        Assert.Equal(a, b);
    }
}
