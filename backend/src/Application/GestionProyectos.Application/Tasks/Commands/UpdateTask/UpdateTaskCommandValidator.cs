using FluentValidation;

namespace GestionProyectos.Application.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la tarea es obligatorio.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El titulo de la tarea es obligatorio.")
            .MaximumLength(200).WithMessage("El titulo no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripcion de la tarea es obligatoria.")
            .MaximumLength(2000).WithMessage("La descripcion no puede superar los 2000 caracteres.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("La prioridad de la tarea no es valida.");
    }
}
