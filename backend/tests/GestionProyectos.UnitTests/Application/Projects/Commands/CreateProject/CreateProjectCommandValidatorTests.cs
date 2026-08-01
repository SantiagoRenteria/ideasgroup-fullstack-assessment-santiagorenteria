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
        var command = new CreateProjectCommand(
            "Nombre", "Descripcion", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.Planned);

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
}
