using FluentValidation;

namespace GestionProyectos.Application.Projects.Queries.ListProjects;

public class ListProjectsQueryValidator : AbstractValidator<ListProjectsQuery>
{
    public const int MaxPageSize = 100;

    public ListProjectsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
    }
}
