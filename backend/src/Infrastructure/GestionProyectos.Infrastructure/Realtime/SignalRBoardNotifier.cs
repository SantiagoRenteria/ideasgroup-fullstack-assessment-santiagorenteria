using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GestionProyectos.Infrastructure.Realtime;

// Adaptador del puerto IBoardNotifier (Application) -- ver
// docs/decisions/arquitectura-decisiones.md §15.1: vive en Infrastructure junto al resto
// de adaptadores externos (EF Core, JWT), no en API.
public class SignalRBoardNotifier : IBoardNotifier
{
    private readonly IHubContext<BoardHub> _hubContext;

    public SignalRBoardNotifier(IHubContext<BoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task TaskCreatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken) =>
        ClientsFor(projectId, excludeConnectionId).SendAsync("TaskCreated", task, cancellationToken);

    public Task TaskUpdatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken) =>
        ClientsFor(projectId, excludeConnectionId).SendAsync("TaskUpdated", task, cancellationToken);

    public Task TaskDeletedAsync(Guid projectId, Guid taskId, Guid columnId, string? excludeConnectionId, CancellationToken cancellationToken) =>
        ClientsFor(projectId, excludeConnectionId).SendAsync("TaskDeleted", new { taskId, columnId }, cancellationToken);

    public Task TaskMovedAsync(Guid projectId, TaskResponseDto task, int targetIndex, string? excludeConnectionId, CancellationToken cancellationToken) =>
        ClientsFor(projectId, excludeConnectionId).SendAsync("TaskMoved", new { task, targetIndex }, cancellationToken);

    // Excluye al emisor cuando tiene el canal abierto (ADR §15.3: ya aplico el cambio de
    // forma optimista con la respuesta HTTP). Sin ConnectionId (cliente sin tiempo real
    // activo todavia), se envia a todo el grupo.
    private IClientProxy ClientsFor(Guid projectId, string? excludeConnectionId)
    {
        var group = BoardHub.GroupName(projectId);

        return string.IsNullOrEmpty(excludeConnectionId)
            ? _hubContext.Clients.Group(group)
            : _hubContext.Clients.GroupExcept(group, new[] { excludeConnectionId });
    }
}
