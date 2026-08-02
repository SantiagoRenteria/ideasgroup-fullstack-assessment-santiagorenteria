using FluentValidation;

namespace GestionProyectos.Application.Reports.Queries.ExportProjectReport;

public class ExportProjectReportQueryValidator : AbstractValidator<ExportProjectReportQuery>
{
    public ExportProjectReportQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Format).NotEmpty();
    }
}
