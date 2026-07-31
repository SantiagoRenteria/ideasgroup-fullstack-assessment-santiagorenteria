using MediatR;

namespace GestionProyectos.Application.Common.Messaging;

// Marca la intencion de solo-lectura frente a ICommand<TResponse>. Ver ICommand.cs.
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
