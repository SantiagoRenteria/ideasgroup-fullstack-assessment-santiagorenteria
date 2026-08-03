using System.Threading.Channels;
using GestionProyectos.Application.Common.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Infrastructure.Realtime;

// El "cuando corre un ciclo" del Outbox Pattern (ADR §24) -- polling + senal in-process
// (Channel), no un bus de mensajeria: decision confirmada tras evaluar RabbitMQ y
// descartarlo por segunda vez (ver §1 y §24). El "que hace un ciclo" vive en
// OutboxProcessor (testeable por separado, sin levantar este BackgroundService).
public class OutboxDispatcher : BackgroundService, IOutboxSignal
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly Channel<bool> _signalChannel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Signal() => _signalChannel.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Fallo procesando el outbox de notificaciones del tablero");
            }

            await WaitForNextCycleAsync(stoppingToken);
        }
    }

    private async Task WaitForNextCycleAsync(CancellationToken stoppingToken)
    {
        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var delayTask = Task.Delay(PollInterval, delayCts.Token);
        var signalTask = _signalChannel.Reader.WaitToReadAsync(delayCts.Token).AsTask();

        try
        {
            await Task.WhenAny(delayTask, signalTask);
        }
        catch (OperationCanceledException)
        {
            // Esperado al detener el host; el while externo sale por stoppingToken.
        }
        finally
        {
            delayCts.Cancel();
        }
    }
}
