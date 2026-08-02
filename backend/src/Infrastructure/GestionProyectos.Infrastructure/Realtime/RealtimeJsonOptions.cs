using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionProyectos.Infrastructure.Realtime;

// Extraido para que sea testeable sin levantar SignalR: un test unitario serializa un DTO
// real con estas opciones y verifica enums-como-string + camelCase, en vez de solo
// confiar en que la configuracion inline de AddJsonProtocol nunca se desincronice de
// Program.cs (bug real: sin JsonStringEnumConverter, Priority viajaba como el entero
// subyacente y el frontend, que indexa TASK_PRIORITY_LABELS por el string del enum, no
// encontraba la clave -- el tag de prioridad desaparecia en las demas sesiones).
public static class RealtimeJsonOptions
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
