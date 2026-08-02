using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestionProyectos.Infrastructure.Realtime;

// Extraido para testear la serializacion sin levantar SignalR: evito el bug real donde
// Priority viajaba como entero y el frontend (indexa por el string del enum) lo perdia.
public static class RealtimeJsonOptions
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
