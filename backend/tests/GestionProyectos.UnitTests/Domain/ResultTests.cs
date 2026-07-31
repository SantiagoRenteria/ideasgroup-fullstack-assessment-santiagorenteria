using GestionProyectos.Domain.Common;
using Xunit;

namespace GestionProyectos.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_MarcaIsSuccessTrue_ConValor()
    {
        var result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_MarcaIsSuccessFalse_ConError()
    {
        var result = Result<string>.Failure("algo salio mal");

        Assert.False(result.IsSuccess);
        Assert.Equal("algo salio mal", result.Error);
        Assert.Null(result.Value);
    }
}
