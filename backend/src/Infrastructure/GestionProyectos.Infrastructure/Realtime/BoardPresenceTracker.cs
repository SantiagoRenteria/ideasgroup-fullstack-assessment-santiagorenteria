using System.Collections.Concurrent;

namespace GestionProyectos.Infrastructure.Realtime;

// En memoria, una sola instancia (Singleton, ver DependencyInjection.cs) -- no distribuido.
// Aceptable para el alcance de esta evaluacion (un solo proceso de API); en un despliegue
// con varias instancias detras de un balanceador haria falta un backplane compartido
// (ej. Redis), el mismo tipo de limite que ya tiene el resto del tiempo real sin backplane.
public class BoardPresenceTracker : IBoardPresenceTracker
{
    // projectId -> (connectionId -> nombre del usuario). Un ConcurrentDictionary por
    // tablero: SignalR puede invocar Join/Leave/OnDisconnected de conexiones distintas en
    // paralelo, sin turno garantizado.
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, string>> _boards = new();

    // connectionId -> projectId: unico lookup que permite a OnDisconnectedAsync limpiar
    // sin que el hub tenga que recordar a que tablero se unio cada conexion.
    private readonly ConcurrentDictionary<string, Guid> _connectionBoards = new();

    public IReadOnlyList<string> Join(Guid projectId, string connectionId, string userName)
    {
        var connections = _boards.GetOrAdd(projectId, _ => new ConcurrentDictionary<string, string>());
        connections[connectionId] = userName;
        _connectionBoards[connectionId] = projectId;

        return NamesOf(connections);
    }

    public IReadOnlyList<string> Leave(Guid projectId, string connectionId)
    {
        _connectionBoards.TryRemove(connectionId, out _);

        if (!_boards.TryGetValue(projectId, out var connections))
            return Array.Empty<string>();

        connections.TryRemove(connectionId, out _);

        return NamesOf(connections);
    }

    public (Guid ProjectId, IReadOnlyList<string> RemainingUsers)? RemoveConnection(string connectionId)
    {
        if (!_connectionBoards.TryRemove(connectionId, out var projectId))
            return null;

        if (!_boards.TryGetValue(projectId, out var connections))
            return (projectId, Array.Empty<string>());

        connections.TryRemove(connectionId, out _);

        return (projectId, NamesOf(connections));
    }

    // Distinct: el mismo usuario con dos pestañas abiertas en el mismo tablero cuenta como
    // dos conexiones pero debe aparecer una sola vez en el indicador.
    private static IReadOnlyList<string> NamesOf(ConcurrentDictionary<string, string> connections) =>
        connections.Values.Distinct().OrderBy(name => name, StringComparer.Ordinal).ToList();
}
