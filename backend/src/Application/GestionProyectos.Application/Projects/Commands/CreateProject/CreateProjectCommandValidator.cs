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
        //
        // Margen de 1 dia (no "hoy" exacto): el frontend envia la fecha en formato
        // yyyy-MM-dd extraida de los componentes LOCALES del navegador (ver
        // ProjectFormComponent.toDateOnly), sin ninguna referencia de zona horaria.
        // Comparar ese valor contra DateTime.UtcNow del servidor sin margen rechaza
        // "hoy" para cualquier usuario en una zona horaria detras de UTC (ej. Ecuador,
        // UTC-5, el contexto real del enunciado) apenas pasa la medianoche UTC, aunque
        // localmente siga siendo el mismo dia. Un dia de margen absorbe ese desfase sin
        // necesidad de modelar una zona horaria de negocio explicita.
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1))
            .WithMessage("La fecha de inicio no puede ser anterior a hoy.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("La fecha de fin prevista no puede ser anterior a la fecha de inicio.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("El estado del proyecto no es válido.");
    }
}
