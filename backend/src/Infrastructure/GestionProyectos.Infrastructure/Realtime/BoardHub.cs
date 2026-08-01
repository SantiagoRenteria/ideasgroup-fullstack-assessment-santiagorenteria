using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GestionProyectos.Infrastructure.Realtime;

// Canal autenticado con el mismo JWT de sesion (seccion 6.2) -- [Authorize] rechaza la
// conexion si el token no es valido. Un grupo por tablero (= por proyecto, seccion 6.7,
// "una sesion no recibe eventos de tableros a los que no esta suscrita").
[Authorize]
public class BoardHub : Hub
{
    public static string GroupName(Guid projectId) => $"board-{projectId}";

    public Task JoinBoard(Guid projectId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));

    public Task LeaveBoard(Guid projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));
}
