using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GestionProyectos.Infrastructure.Realtime;

// Canal autenticado con el mismo JWT de sesion (seccion 6.2) -- [Authorize] rechaza la
// conexion si el token no es valido. Un grupo por tablero (= por proyecto, seccion 6.7,
// "una sesion no recibe eventos de tableros a los que no esta suscrita").
[Authorize]
public class BoardHub : Hub
{
    private readonly IBoardPresenceTracker _presenceTracker;

    public BoardHub(IBoardPresenceTracker presenceTracker)
    {
        _presenceTracker = presenceTracker;
    }

    public static string GroupName(Guid projectId) => $"board-{projectId}";

    // A diferencia de los eventos Task* (que excluyen al emisor), aca el usuario que se
    // une SI debe verse a si mismo en la lista de conectados.
    public async Task JoinBoard(Guid projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));

        var userName = Context.User?.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? "Usuario";
        var connectedUsers = _presenceTracker.Join(projectId, Context.ConnectionId, userName);

        await Clients.Group(GroupName(projectId)).SendAsync("BoardPresenceChanged", connectedUsers);
    }

    public async Task LeaveBoard(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));

        var connectedUsers = _presenceTracker.Leave(projectId, Context.ConnectionId);

        await Clients.Group(GroupName(projectId)).SendAsync("BoardPresenceChanged", connectedUsers);
    }

    // Cierre de pestaña, perdida de red, etc. -- sin esto, un usuario que se va sin pasar
    // por LeaveBoard quedaria "conectado" para siempre en el indicador de los demas.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var result = _presenceTracker.RemoveConnection(Context.ConnectionId);

        if (result is not null)
        {
            await Clients.Group(GroupName(result.Value.ProjectId)).SendAsync("BoardPresenceChanged", result.Value.RemainingUsers);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
