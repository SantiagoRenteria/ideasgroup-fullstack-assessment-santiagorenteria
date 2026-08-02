namespace GestionProyectos.Infrastructure.Realtime;

// Puramente infraestructura del hub -- ningun caso de uso de Application necesita saber
// quien esta conectado a un tablero, asi que este puerto vive en Infrastructure, no en
// Application/Common/Interfaces (a diferencia de IBoardNotifier, que si cruza esa
// frontera porque los CommandHandlers de tareas lo usan).
public interface IBoardPresenceTracker
{
    // Devuelve la lista de nombres conectados al tablero DESPUES de aplicar el cambio,
    // para que BoardHub pueda transmitirla en una sola llamada.
    IReadOnlyList<string> Join(Guid projectId, string connectionId, string userName);

    IReadOnlyList<string> Leave(Guid projectId, string connectionId);

    // Al desconectarse (cerrar la pestaña sin pasar por LeaveBoard), el hub no sabe a que
    // tablero pertenecia la conexion sin este lookup inverso. Null si la conexion no
    // estaba unida a ningun tablero.
    (Guid ProjectId, IReadOnlyList<string> RemainingUsers)? RemoveConnection(string connectionId);
}
