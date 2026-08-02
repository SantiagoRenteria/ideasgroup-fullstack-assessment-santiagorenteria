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
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void Failure_MarcaIsSuccessFalse_ConErrorYErrorType()
    {
        var result = Result<string>.Failure("algo salio mal", ErrorType.Conflict);

        Assert.False(result.IsSuccess);
        Assert.Equal("algo salio mal", result.Error);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Unauthorized)]
    public void Failure_SinGenerico_PreservaErrorType(ErrorType errorType)
    {
        var result = Result.Failure("algo salio mal", errorType);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorType, result.ErrorType);
    }
}
