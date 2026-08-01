using FluentValidation;

namespace GestionProyectos.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del proyecto es obligatoria.")
            .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.");

        // Solo al crear -- Update no la exige, para no bloquear la edicion de un
        // proyecto cuya fecha de inicio ya paso (ver UpdateProjectCommandValidator).
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de inicio no puede ser anterior a hoy.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("La fecha de fin prevista no puede ser anterior a la fecha de inicio.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("El estado del proyecto no es válido.");
    }
}
