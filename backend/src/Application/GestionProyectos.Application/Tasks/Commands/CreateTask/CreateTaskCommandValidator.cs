using FluentValidation;

namespace GestionProyectos.Application.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("La tarea debe pertenecer a una columna.");

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
