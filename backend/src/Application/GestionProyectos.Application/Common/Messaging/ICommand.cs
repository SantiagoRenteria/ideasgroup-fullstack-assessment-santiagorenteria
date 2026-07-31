using MediatR;

namespace GestionProyectos.Application.Common.Messaging;

// Marca la intencion de escritura/accion frente a IQuery<TResponse>. MediatR no distingue
// Command de Query por tipos (ambos son IRequest<T>); estas interfaces existen para que esa
// distincion sea explicita y verificable por el compilador, no solo por convencion de nombres.
public interface ICommand<TResponse> : IRequest<TResponse>
{
}
