using FluentValidation;

namespace GestionProyectos.Application.Columns.Commands.CreateColumn;

public class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
{
    public CreateColumnCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("La columna debe pertenecer a un proyecto.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la columna es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("El orden de la columna no puede ser negativo.");
    }
}
