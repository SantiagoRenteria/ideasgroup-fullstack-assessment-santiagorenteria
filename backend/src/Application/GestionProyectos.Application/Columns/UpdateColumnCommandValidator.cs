using FluentValidation;

namespace GestionProyectos.Application.Columns;

public class UpdateColumnCommandValidator : AbstractValidator<UpdateColumnCommand>
{
    public UpdateColumnCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador de la columna es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la columna es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("El orden de la columna no puede ser negativo.");
    }
}
