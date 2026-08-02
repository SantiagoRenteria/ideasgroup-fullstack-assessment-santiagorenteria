using System.Text.Json;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Realtime;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure;

public class RealtimeJsonOptionsTests
{
    // Regresion: sin JsonStringEnumConverter, Priority serializaba como el entero
    // subyacente por el hub de SignalR (a diferencia de la API REST, que si lo tenia via
    // Program.cs) y el frontend, que indexa TASK_PRIORITY_LABELS/SEVERITY por el string
    // del enum, no encontraba la clave -- el tag de prioridad desaparecia en las demas
    // sesiones al recibir TaskCreated/Updated/Moved en tiempo real.
    [Fact]
    public void Configure_SerializaEnumsComoStringYPropiedadesEnCamelCase()
    {
        var options = new JsonSerializerOptions();
        RealtimeJsonOptions.Configure(options);

        var dto = new TaskResponseDto(Guid.NewGuid(), Guid.NewGuid(), "Titulo", "Descripcion", TaskPriority.High, null, "m", DateTime.UtcNow);

        var json = JsonSerializer.Serialize(dto, options);

        Assert.Contains("\"priority\":\"High\"", json);
        Assert.Contains("\"columnId\"", json);
        Assert.DoesNotContain("\"ColumnId\"", json);
    }
}
