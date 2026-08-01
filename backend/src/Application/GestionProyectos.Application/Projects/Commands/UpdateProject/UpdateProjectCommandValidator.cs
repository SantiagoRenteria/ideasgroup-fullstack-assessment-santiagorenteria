using FluentValidation;

namespace GestionProyectos.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del proyecto es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del proyecto es obligatoria.")
            .MaximumLength(2000).WithMessage("La descripción no puede superar los 2000 caracteres.");

        // A diferencia de CreateProjectCommandValidator, aqui no se exige StartDate >= hoy
        // a proposito: un proyecto que ya inicio conserva una fecha de inicio en el pasado
        // por diseno, y bloquearla impediria editar cualquier otro campo de un proyecto en curso.
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("La fecha de fin prevista no puede ser anterior a la fecha de inicio.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("El estado del proyecto no es válido.");
    }
}
