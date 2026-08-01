using GestionProyectos.Application.Projects.Commands.CreateProject;
using GestionProyectos.Domain.Enums;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ConDatosValidos_NoTieneErrores()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new CreateProjectCommand("Nombre", "Descripcion", hoy, hoy.AddMonths(6), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ConFechaFinAnteriorAInicio_TieneError()
    {
        var command = new CreateProjectCommand(
            "Nombre", "Descripcion", new DateOnly(2026, 6, 30), new DateOnly(2026, 1, 1), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.EndDate));
    }

    [Fact]
    public void Validate_ConNombreVacio_TieneError()
    {
        var command = new CreateProjectCommand(
            "", "Descripcion", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Name));
    }

    [Fact]
    public void Validate_ConFechaDeInicioDeHaceDosDias_TieneError()
    {
        // Fuera del margen de 1 dia que absorbe el desfase de zona horaria entre el
        // "hoy" local del navegador y el UtcNow del servidor (ver el comentario en
        // CreateProjectCommandValidator).
        var haceDosDias = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var command = new CreateProjectCommand("Nombre", "Descripcion", haceDosDias, haceDosDias.AddMonths(1), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.StartDate));
    }

    [Fact]
    public void Validate_ConFechaDeInicioAyer_NoTieneError()
    {
        // Dentro del margen de 1 dia: un usuario en una zona horaria detras de UTC
        // (ej. Ecuador, UTC-5) puede tener "hoy" localmente cuando el servidor ya
        // considera que es "ayer" en UTC. No debe rechazarse.
        var ayer = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var command = new CreateProjectCommand("Nombre", "Descripcion", ayer, ayer.AddMonths(1), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ConFechaDeInicioHoy_NoTieneError()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new CreateProjectCommand("Nombre", "Descripcion", hoy, hoy.AddMonths(1), ProjectStatus.Planned);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
