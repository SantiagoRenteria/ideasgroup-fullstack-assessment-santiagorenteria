using FluentValidation;

namespace GestionProyectos.Application.Tasks.Commands.MoveTask;

public class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la tarea es obligatorio.");

        RuleFor(x => x.TargetColumnId)
            .NotEmpty().WithMessage("La columna destino es obligatoria.");

        RuleFor(x => x.TargetIndex)
            .GreaterThanOrEqualTo(0).WithMessage("La posicion destino no puede ser negativa.");
    }
}
